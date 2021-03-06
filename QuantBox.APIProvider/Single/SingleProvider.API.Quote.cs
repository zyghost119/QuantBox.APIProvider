using QuantBox.Extensions;
using SmartQuant;
using System;
using XAPI;
using XAPI.Callback;

namespace QuantBox.APIProvider.Single
{
    public partial class SingleProvider
    {
        private QuoteField ToQuoteStruct(ExecutionCommand command, string apiSymbol, string apiExchange, out Order askOrder, out Order bidOrder)
        {
            bidOrder = command.Order;
            askOrder = (Order)bidOrder.GetSameTimeOrder();

            QuoteField field = new QuoteField();
            field.InstrumentID = apiSymbol;
            field.ExchangeID = apiExchange;

            field.AskQty = askOrder.Qty;
            field.AskPrice = askOrder.Price;
            field.AskOpenClose = GetOpenClose(askOrder);
            field.AskHedgeFlag = GetHedgeFlag(askOrder);

            field.BidQty = bidOrder.Qty;
            field.BidPrice = bidOrder.Price;
            field.BidOpenClose = GetOpenClose(bidOrder);
            field.BidHedgeFlag = GetHedgeFlag(bidOrder);

            field.QuoteReqID = bidOrder.GetQuoteReqID();
            //field.Account = command.Order.Account;

            return field;
        }

        private void CmdNewQuote(ExecutionCommand command)
        {
            string altSymbol;
            string altExchange;
            string apiSymbol;
            string apiExchange;
            double apiTickSize;
            string apiProductID;

            GetApi_Symbol_Exchange_TickSize(command.Instrument, this.id,
                out altSymbol, out altExchange,
                out apiSymbol, out apiExchange,
                out apiTickSize,
                out apiProductID);

            Order bidOrder;
            Order askOrder;

            QuoteField field = ToQuoteStruct(command, apiSymbol, apiExchange, out askOrder, out bidOrder);

            quoteMap.DoQuoteSend(field, command, askOrder, bidOrder);
        }

        private void CmdCancelQuote(ExecutionCommand command)
        {
            quoteMap.DoQuoteCancel(command);
        }

        private void OnRtnQuote_callback(object sender, ref QuoteField quote)
        {
            (sender as XApi).GetLog().Debug("OnRtnQuote:" + quote.ToFormattedString());
            try
            {
                quoteMap.Process(ref quote);
            }
            catch (Exception ex)
            {
                (sender as XApi).GetLog().Error(ex);
            }
        }

    }
}
