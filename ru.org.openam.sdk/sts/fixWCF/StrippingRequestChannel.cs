//  http://java.net/jira/browse/WSIT-1066
//  http://social.msdn.microsoft.com/Forums/en-US/wcf/thread/371184de-5c05-4c70-8899-13536d8f5d16
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.ServiceModel.Configuration;

namespace ru.org.openam.sdk.sts.fixWCF
{
    class StrippingRequestChannel : IRequestChannel
    {
        IRequestChannel _innerChannel = null;

        public StrippingRequestChannel(IRequestChannel innerchannel)
        {
            _innerChannel = innerchannel;
            // hook up event handlers
            innerchannel.Closed += (sender, e) => { if (Closed != null) Closed(sender, e); };
            innerchannel.Closing += (sender, e) => { if (Closing != null) Closing(sender, e); };
            innerchannel.Faulted += (sender, e) => { if (Faulted != null) Faulted(sender, e); };
            innerchannel.Opened += (sender, e) => { if (Opened != null) Opened(sender, e); };
            innerchannel.Opening += (sender, e) => { if (Opening != null) Opening(sender, e); };
        }

        #region IRequestChannel Members

        public IAsyncResult BeginRequest(System.ServiceModel.Channels.Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerChannel.BeginRequest(message, timeout, callback, state);
        }

        public IAsyncResult BeginRequest(System.ServiceModel.Channels.Message message, AsyncCallback callback, object state)
        {
            return _innerChannel.BeginRequest(message, callback, state);
        }

        public System.ServiceModel.Channels.Message EndRequest(IAsyncResult result)
        {
            return _innerChannel.EndRequest(result);
        }

        public EndpointAddress RemoteAddress
        {
            get { return _innerChannel.RemoteAddress; }
        }

        // here must we process the request
        public System.ServiceModel.Channels.Message Request(System.ServiceModel.Channels.Message message, TimeSpan timeout)
        {
            System.ServiceModel.Channels.Message ret = null;
            // get response first
            ret = _innerChannel.Request(message, timeout);

            // need to create a copy first
            MessageBuffer buffer = ret.CreateBufferedCopy(int.MaxValue);


            MemoryStream stream = new MemoryStream(1024);
            buffer.WriteMessage(stream);
            stream.Position = 0;

            // process XML using XmlDocument and XPathNavigator
            // note that this is not the most efficient way, but it's the simplest to demonstrate
            XmlDocument doc = new XmlDocument();
            doc.Load(stream);

            XPathNavigator n = doc.CreateNavigator();

            //if (n.MoveToFollowing("BinarySecurityToken", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"))
            //  n.DeleteSelf();

            //if (n.MoveToFollowing("Signature", "http://www.w3.org/2000/09/xmldsig#"))
            //  n.DeleteSelf();
            //if (n.MoveToFollowing("Action", "http://www.w3.org/2005/08/addressing"))
            //{
            //    System.Diagnostics.Trace.TraceInformation("strip To");
            //    n.DeleteSelf();
            //}
            //if (n.MoveToFollowing("To", "http://www.w3.org/2005/08/addressing"))
            //{
            //    System.Diagnostics.Trace.TraceInformation("strip To");
            //    n.DeleteSelf();
            //}
            //if (n.MoveToFollowing("MessageID", "http://www.w3.org/2005/08/addressing"))
            //{
            //    System.Diagnostics.Trace.TraceInformation("strip To");
            //    n.DeleteSelf();
            //}
            //if (n.MoveToFollowing("RelatesTo", "http://www.w3.org/2005/08/addressing"))
            //{
            //    System.Diagnostics.Trace.TraceInformation("strip To");
            //    n.DeleteSelf();
            //}
            //if (n.MoveToFollowing("Body", "http://www.w3.org/2003/05/soap-envelope"))
            //{
            //    System.Diagnostics.Trace.TraceInformation("strip To");
            //    n.DeleteSelf();
            //}
            if (n.MoveToFollowing("Timestamp", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"))
            {
                System.Diagnostics.Trace.TraceInformation("strip Timestamp");
                n.DeleteSelf();
            }

            n.MoveToRoot();
            XmlReader reader = n.ReadSubtree();

            // create message
            System.ServiceModel.Channels.Message strippedMessage = System.ServiceModel.Channels.Message.CreateMessage(reader, int.MaxValue, ret.Version);
            return strippedMessage;
        }

        public System.ServiceModel.Channels.Message Request(System.ServiceModel.Channels.Message message)
        {
            System.ServiceModel.Channels.Message ret = Request(message, new TimeSpan(0, 1, 30));
            return ret;
        }

        public Uri Via
        {
            get { return _innerChannel.Via; }
        }

        #endregion

        #region IChannel Members

        public T GetProperty<T>() where T : class
        {
            return _innerChannel.GetProperty<T>();
        }

        #endregion

        #region ICommunicationObject Members

        public void Abort()
        {
            _innerChannel.Abort();
        }

        public IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerChannel.BeginClose(timeout, callback, state);
        }

        public IAsyncResult BeginClose(AsyncCallback callback, object state)
        {
            return _innerChannel.BeginClose(callback, state);
        }

        public IAsyncResult BeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerChannel.BeginOpen(timeout, callback, state);
        }

        public IAsyncResult BeginOpen(AsyncCallback callback, object state)
        {
            return _innerChannel.BeginOpen(callback, state);
        }

        public void Close(TimeSpan timeout)
        {
            _innerChannel.Close(timeout);
        }

        public void Close()
        {
            _innerChannel.Close();
        }

        public event EventHandler Closed;

        public event EventHandler Closing;

        public void EndClose(IAsyncResult result)
        {
            _innerChannel.EndClose(result);
        }

        public void EndOpen(IAsyncResult result)
        {
            _innerChannel.EndOpen(result);
        }

        public event EventHandler Faulted;

        public void Open(TimeSpan timeout)
        {
            _innerChannel.Open(timeout);
        }

        public void Open()
        {
            _innerChannel.Open();
        }

        public event EventHandler Opened;

        public event EventHandler Opening;

        public CommunicationState State
        {
            get { return _innerChannel.State; }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            IDisposable d = _innerChannel as IDisposable;
            if (d != null)
            {
                d.Dispose();
            }
        }

        #endregion
    }

    class StrippingChannelFactory<TChannel> : ChannelFactoryBase<TChannel>
    {
        private IChannelFactory<TChannel> innerChannelFactory;

        public IChannelFactory<TChannel> InnerChannelFactory
        {
            get { return this.innerChannelFactory; }
            set { this.innerChannelFactory = value; }
        }

        public StrippingChannelFactory()
        { }

        protected override void OnOpen(TimeSpan timeout)
        {
            this.innerChannelFactory.Open(timeout);
        }

        protected override TChannel OnCreateChannel(EndpointAddress to, Uri via)
        {
            TChannel innerchannel = this.innerChannelFactory.CreateChannel(to, via);
            if (typeof(TChannel) == typeof(IRequestChannel))
            {
                StrippingRequestChannel CachereqCnl = new StrippingRequestChannel((IRequestChannel)innerchannel);
                return (TChannel)(object)CachereqCnl;
            }
            return innerchannel;
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            throw new NotImplementedException();
        }
    }

    class StrippingChannelBindingElement : BindingElement
    {
        public StrippingChannelBindingElement()
        {
        }

        protected StrippingChannelBindingElement(StrippingChannelBindingElement other)
            : base(other)
        {
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            StrippingChannelFactory<TChannel> Cachecf = new StrippingChannelFactory<TChannel>();
            Cachecf.InnerChannelFactory = context.BuildInnerChannelFactory<TChannel>();
            return Cachecf;
        }

        public override BindingElement Clone()
        {
            StrippingChannelBindingElement other = new StrippingChannelBindingElement(this);
            return other;
        }

        public override T GetProperty<T>(BindingContext context)
        {
            return context.GetInnerProperty<T>();
        }
    }

    class StrippingChannelBindingElementExtensionElement : BindingElementExtensionElement
    {
        public override Type BindingElementType
        {
            get { return typeof(StrippingChannelBindingElement); }
        }

        protected override BindingElement CreateBindingElement()
        {
            return new StrippingChannelBindingElement();
        }
    }
}
