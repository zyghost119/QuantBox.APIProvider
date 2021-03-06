using System;
using System.Collections.Generic;


using SmartQuant;
using System.IO;
using Newtonsoft.Json;
using System.ComponentModel;
using QuantBox.APIProvider.Single;
using System.Reflection;
using CommandLine;
using ClipboardMonitor;
using System.Threading;


namespace QuantBox.APIProvider
{
    /// <summary>
    /// Provder宿主
    /// 由它进行其它Provder的初始创建，以及订单的路由
    /// </summary>
    public partial class ProviderHost : Provider, IExecutionProvider, IDataProvider
    {
        #region Provider
        [Description("创建Provder的记录文件路径")]
        public string ConfigPath { get; private set; }


        [Description("是否自动使用相对路径来查找API")]
        public bool AutoMakeRelativePath
        {
            get { return autoMakeRelativePath; }
            set
            {
                autoMakeRelativePath = value;
            }
        }

        public static bool autoMakeRelativePath;


        [Description("Provder列表，可在此编辑")]
        public BindingList<ProviderItem> ProviderList { get; set; }

        [Category(CATEGORY_INFO)]
        [Description("插件版本信息")]
        public string Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        public ProviderHost(Framework framework)
            : base(framework)
        {
            Init(100, "API Host");

            ConfigPath = Path.Combine(Installation.ConfigDir.FullName, "API_Host.json");
            ProviderList = new BindingList<ProviderItem>();

            Load(ConfigPath);
            if (ProviderList == null)
                ProviderList = new BindingList<ProviderItem>();

            string applicationDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            Create(ProviderList);

            ProviderList.ListChanged += ProviderList_ListChanged;

#if NET48
            cmdLine = new CmdLine();
            try
            {
                cmdLine.ParseForStart(this);
            }
            catch
            {

            }
            

            new ClipboardNotifications();
            ClipboardNotifications.ClipboardUpdate += ClipboardNotifications_ClipboardUpdate;
#endif
        }

        ~ProviderHost()
        {
            Save(ConfigPath);
        }

        void ProviderList_ListChanged(object sender, ListChangedEventArgs e)
        {
            Save(ConfigPath);
        }

        public void Init(byte id, string name)
        {
            base.id = id;
            base.name = name;
            base.description = "QuantBox API Provider Host";
            base.url = "www.quantbox.cn";
        }

        /// <summary>
        /// 指定保存格式
        /// </summary>
        private static readonly JsonSerializerSettings JSetting = new JsonSerializerSettings()
        {
            //NullValueHandling = NullValueHandling.Ignore,
            //DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.Indented,
        };

        // 读取信息
        public void Load(string path)
        {
            try
            {
                object ret;
                using (TextReader reader = new StreamReader(path))
                {
                    ret = JsonConvert.DeserializeObject(reader.ReadToEnd(), ProviderList.GetType());
                    reader.Close();
                }
                ProviderList = ret as BindingList<ProviderItem>;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Save(string path)
        {
            if (ProviderList == null)
                return;

            using (TextWriter writer = new StreamWriter(path))
            {
                writer.Write("{0}", JsonConvert.SerializeObject(ProviderList, ProviderList.GetType(), JSetting));
                writer.Close();
            }
        }

        public override void Connect()
        {
            base.Connect();
            Create(ProviderList);
            Save(ConfigPath);
        }

        public override void Disconnect()
        {
            base.Disconnect();
            Save(ConfigPath);
        }

        public void Create(IList<ProviderItem> list)
        {
            foreach (var l in list)
            {
                IProvider pvd = framework.ProviderManager.GetProvider(l.Id);
                if (pvd == null)
                {
                    SingleProvider provider = new SingleProvider(framework);
                    provider.Init(l.Id, l.Name);
                    framework.ProviderManager.AddProvider(provider);
                }
            }
        }
        #endregion

        #region IExecutionProvider
        [Description("默认ExecutionProvider的ID")]
        public byte DefaultExecutionProvider { get; set; }

        [Description("默认ExecutionProvider")]
        public IExecutionProvider ExecutionProvider
        {
            get
            {
                if (DefaultExecutionProvider == id)
                    return null;

                return framework.ProviderManager.GetExecutionProvider(DefaultExecutionProvider);
            }
        }
        public override void Send(ExecutionCommand command)
        {
            if (command.RouteId == id)
                return;

            IExecutionProvider provider;
            if (command.RouteId != 0)
            {
                provider = framework.ProviderManager.GetExecutionProvider(command.RouteId);
            }
            else
            {
                provider = ExecutionProvider;
            }
            provider.Send(command);
        }
        #endregion

        #region IDataProvider
        [Description("默认DataProvider的ID")]
        public byte DefaultDataProvider { get; set; }

        [Description("默认DataProvider")]
        public IDataProvider DataProvider
        {
            get
            {
                return framework.ProviderManager.GetDataProvider(DefaultDataProvider);
            }
        }
        public override void Subscribe(Instrument instrument)
        {
            // 行情订阅最好还是使用Instrument中指定DataProvider的方式比较好
            DataProvider.Subscribe(instrument);
        }
        public override void Subscribe(InstrumentList instrument)
        {
            DataProvider.Subscribe(instrument);
        }
        public override void Unsubscribe(Instrument instrument)
        {
            DataProvider.Unsubscribe(instrument);
        }
        public override void Unsubscribe(InstrumentList instrument)
        {
            DataProvider.Unsubscribe(instrument);
        }
        #endregion
    }
}
