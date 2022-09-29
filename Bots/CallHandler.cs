using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Core.Serialization;
using Microsoft.Graph.Communications.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CallingBotSample.Bots
{
    public class CallHandler : IDisposable
    {
        public ICall _call { get; }
        protected CallingBot _callingBot { get; }
        protected CommsSerializer _serializer { get; }

        public LinkedList<string> _outcomesLogMostRecentFirst { get; } = new LinkedList<string>();

        public CallHandler(CallingBot bot, ICall call, CommsSerializer serializer)
        {
            this._callingBot = bot;
            this._call = call;

            var outcome = serializer.SerializeObject(call.Resource);
            this._outcomesLogMostRecentFirst.AddFirst("Call Created:\n" + outcome);

            call.OnUpdated += this.OnCallUpdated;
            call.Participants.OnUpdated += OnParticipantsUpdated; //+= this.OnParticipantsUpdated;
        }

        private void OnParticipantsUpdated(IParticipantCollection sender, CollectionEventArgs<IParticipant> args)
        {
            foreach (var participant in args.AddedResources)
            {
                var outcome = _serializer.SerializeObject(participant.Resource);
                this._outcomesLogMostRecentFirst.AddFirst("Participant Added:\n" + outcome);

                participant.OnUpdated += this.OnParticipantUpdated; //+= this.OnParticipantUpdated;
            }

            foreach (var participant in args.RemovedResources)
            {
                var outcome = _serializer.SerializeObject(participant.Resource);
                this._outcomesLogMostRecentFirst.AddFirst("Participant Removed:\n" + outcome);

                participant.OnUpdated -= this.OnParticipantUpdated;
            }

            this.ParticipantsOnUpdated(sender, args);
        }

        private void OnParticipantUpdated(IParticipant sender, ResourceEventArgs<Participant> args)
        {
            var outcome = _serializer.SerializeObject(sender.Resource);
            this._outcomesLogMostRecentFirst.AddFirst("Participant Updated:\n" + outcome);

            this.ParticipantOnUpdated(sender, args);
        }

        /// <summary>
        /// The event handler when participants are updated.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The arguments</param>
        protected virtual void ParticipantsOnUpdated(IParticipantCollection sender, CollectionEventArgs<IParticipant> args)
        {
            // do nothing in base class.
        }

        /// <summary>
        /// Event handler when participan is updated.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The arguments</param>
        protected virtual void ParticipantOnUpdated(IParticipant sender, ResourceEventArgs<Participant> args)
        {
            // do nothing in base class.
        }
        

        /// <inheritdoc />
        public void Dispose()
        {
            this._call.OnUpdated -= this.OnCallUpdated;
            this._call.Participants.OnUpdated -= this.OnParticipantsUpdated;

            foreach (var participant in this._call.Participants)
            {
                participant.OnUpdated -= this.OnParticipantUpdated;
            }
        }

        /// <summary>
        /// The event handler when call is updated.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The arguments</param>
        protected virtual void CallOnUpdated(ICall sender, ResourceEventArgs<Call> args)
        {
            // do nothing in base class.
        }

        /// <summary>
        /// Event handler for call updated.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">The event arguments.</param>
        private void OnCallUpdated(ICall sender, ResourceEventArgs<Call> args)
        {
            var outcome = _serializer.SerializeObject(sender.Resource);
            this._outcomesLogMostRecentFirst.AddFirst("Call Updated:\n" + outcome);

            this.CallOnUpdated(sender, args);
        }

    
        internal void SubscribeToTone()
        {
            Task.Run(async () =>
            {
                try
                {
                    await this._call.SubscribeToToneAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw;
                }
            });
        }
    }
}
