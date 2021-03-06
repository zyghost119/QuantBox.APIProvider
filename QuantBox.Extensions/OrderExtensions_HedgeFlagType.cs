using XAPI;
using SmartQuant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantBox.Extensions
{
    public static class OrderExtensions_HedgeFlagType
    {
        public static int index = OrderTagType.Network;
        public static Order SetHedgeFlag(this Order order, HedgeFlagType hedgeFlag)
        {
            order.GetDictionary(index)[OrderTagType.HedgeFlag] = (byte)hedgeFlag;
            return order;
        }

        public static HedgeFlagType? GetHedgeFlag(this Order order)
        {
            object obj = order.GetDictionaryValue(OrderTagType.HedgeFlag, index);
            if (obj == null)
            {
                return (HedgeFlagType?)obj;
            }
            return (HedgeFlagType?)(byte)obj;
        }
    }
}
