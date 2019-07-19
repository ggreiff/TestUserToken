using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace TestUserToken
{
    /// <summary>
    /// Class to perform custome message inspection as behaviour.
    /// </summary>
    public class MessageInspectorBehavior : IClientMessageInspector, IEndpointBehavior
    {
        // Acts as the event to notify subscribers of message inspection.
        public event EventHandler<MessageInspectorArgs> OnMessageInspected;

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            if (OnMessageInspected != null)
            {
                // Notify the subscribers of the inpected message.
                OnMessageInspected(this, new MessageInspectorArgs { Message = reply.ToString(), MessageInspectionType = EMessageInspectionType.Response });

            }
        }

        public object BeforeSendRequest(ref Message request, System.ServiceModel.IClientChannel channel)
        {
            if (OnMessageInspected != null)
            {
                // Notify the subscribers of the inpected message.
                OnMessageInspected(this, new MessageInspectorArgs { Message = request.ToString(), MessageInspectionType = EMessageInspectionType.Request });
            }

            MessageBuffer buffer = request.CreateBufferedCopy(Int32.MaxValue);
            request = buffer.CreateMessage();
            //Console.WriteLine("Received:\n{0}", buffer.CreateMessage().ToString());
            return null;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
            // Do nothing.
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            // Add the message inspector to the as part of the service behaviour.
            clientRuntime.MessageInspectors.Add(this);
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            // Do nothing.
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            // Do nothing.
        }
    }

    /// <summary>
    /// Enum representing message inspection types.
    /// </summary>
    public enum EMessageInspectionType { Request = 0, Response = 1 };

    /// <summary>
    /// Class to pass inspection event arguments.
    /// </summary>
    public class MessageInspectorArgs : EventArgs
    {
        /// <summary>
        /// Type of the message inpected.
        /// </summary>
        public EMessageInspectionType MessageInspectionType { get; internal set; }

        /// <summary>
        /// Inspected raw message.
        /// </summary>
        public string Message { get; internal set; }
    }
}
