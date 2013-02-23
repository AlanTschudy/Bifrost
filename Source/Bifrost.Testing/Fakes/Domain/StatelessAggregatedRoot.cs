using System;
using Bifrost.Domain;

namespace Bifrost.Testing.Fakes.Domain
{
    public class StatelessAggregatedRoot : AggregateRoot
    {
        public StatelessAggregatedRoot(Guid id) : base(id) {}
    }
}