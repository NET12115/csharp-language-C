using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace OmniSharp.Extensions.JsonRpc.Testing
{
    public class AggregateSettler : ISettler
    {
        private readonly ISettler[] _settlers;

        public AggregateSettler(params ISettler[] settlers) => _settlers = settlers;

        public Task SettleNext() => Task.WhenAll(_settlers.Select(z => z.SettleNext()));

        public IObservable<Unit> Settle() =>
            _settlers
               .Select((settler, index) => settler.Settle().Select((_, value) => new { index, value }))
               .CombineLatest()
               .Scan(
                    0, (value, result) => {
                        var maxValue = result.Max(z => z.value);
                        return result.All(z => z.value == maxValue) ? maxValue : value;
                    }
                )
               .DistinctUntilChanged()
               .Select(_ => Unit.Default);
    }
}
