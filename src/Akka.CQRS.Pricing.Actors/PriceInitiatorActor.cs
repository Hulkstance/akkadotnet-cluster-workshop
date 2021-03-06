using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Akka.CQRS.Pricing.Commands;
using Akka.Event;

namespace Akka.CQRS.Pricing.Actors
{
    /// <summary>
    /// Intended to be a Cluster Singleton. Responsible for ensuring there's at least one instance
    /// of a <see cref="MatchAggregator"/> for every single persistence id found inside the datastore.
    /// </summary>
    public sealed class PriceInitiatorActor : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly IActorRef _pricingQueryProxy;
        private readonly HashSet<string> _tickers = new HashSet<string>();

        /*
         * Used to periodically ping Akka.Cluster.Sharding and ensure that all pricing
         * entities are up and producing events for their in-memory replicas over the network.
         *
         * Technically, akka.cluster.sharding.remember-entities = on should take care of this
         * for us in the initial pass, but the impact of having this code is virtually zero
         * and in the event of a network partition or an error somewhere, will effectively prod
         * the non-existent entity into action. Worth having it.
         */
        private ICancelable _heartbeatInterval;


        private class Heartbeat
        {
            public static readonly Heartbeat Instance = new Heartbeat();
            private Heartbeat() { }
        }

        public PriceInitiatorActor(IActorRef pricingQueryProxy)
        {
            _pricingQueryProxy = pricingQueryProxy;

            Receive<Ping>(p =>
            {
                _tickers.Add(p.StockId);
                _pricingQueryProxy.Tell(p);
            });

            Receive<Heartbeat>(h =>
            {
                foreach (var p in _tickers)
                {
                    _pricingQueryProxy.Tell(new Ping(p));
                }
            });

            Receive<UnexpectedEndOfStream>(end =>
            {
                _log.Warning("Received unexpected end of PersistenceIds stream. Restarting.");
                throw new ApplicationException("Restart me!");
            });
        }

        protected override void PreStart()
        {
            var self = Self;
            foreach (var t in AvailableTickerSymbols.Symbols)
            {
                self.Tell(new Ping(t));
            }

            _heartbeatInterval = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30), Self, Heartbeat.Instance, ActorRefs.NoSender);
        }

        protected override void PostStop()
        {
            _heartbeatInterval?.Cancel();
        }
    }
}
