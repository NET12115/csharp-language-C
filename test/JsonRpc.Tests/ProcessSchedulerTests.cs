﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;

namespace JsonRpc.Tests
{
    public class ProcessSchedulerTests
    {
        private const int SLEEPTIME_MS = 20;
        private const int ALONGTIME_MS = 500;

        class AllRequestProcessTypes : TheoryData
        {
            public override IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { RequestProcessType.Serial };
                yield return new object[] { RequestProcessType.Parallel };
            }
        }

        [Theory, ClassData(typeof(AllRequestProcessTypes))]
        public void ShouldScheduleCompletedTask(RequestProcessType type)
        {
            using (IScheduler s = new ProcessScheduler())
            {
                var done = new CountdownEvent(1);
                s.Start();
                s.Add(type, () => {
                    done.Signal();
                    return Task.CompletedTask;
                });
                done.Wait(ALONGTIME_MS).Should().Be(true);
            }
        }

        [Theory, ClassData(typeof(AllRequestProcessTypes))]
        public void ShouldScheduleAwaitableTask(RequestProcessType type)
        {
            using (IScheduler s = new ProcessScheduler())
            {
                var done = new CountdownEvent(1);
                s.Start();
                s.Add(RequestProcessType.Serial, async () => {
                    await Task.Yield();
                    done.Signal();
                });
                done.Wait(ALONGTIME_MS).Should().Be(true);
            }
        }

        [Theory, ClassData(typeof(AllRequestProcessTypes))]
        public void ShouldScheduleConstructedTask(RequestProcessType type)
        {
            using (IScheduler s = new ProcessScheduler())
            {
                var done = new CountdownEvent(1);
                s.Start();
                s.Add(RequestProcessType.Serial, () => {
                    return new Task(() => {
                        done.Signal();
                    });
                });
                done.Wait(ALONGTIME_MS).Should().Be(true);
            }
        }

        [Fact]
        public void ShouldScheduleSerialInOrder()
        {
            using (IScheduler s = new ProcessScheduler())
            {
                var done = new CountdownEvent(3); // 3x s.Add
                var running = 0;
                var peek = 0;

                Func<Task> HandlePeek = async () => {                    
                    var p = Interlocked.Increment(ref running);
                    lock (this) peek = Math.Max(peek, p);
                    await Task.Delay(SLEEPTIME_MS); // give a different HandlePeek task a chance to run
                    Interlocked.Decrement(ref running);
                    done.Signal();
                };

                s.Start();
                for (var i = 0; i < done.CurrentCount; i++)
                    s.Add(RequestProcessType.Serial, HandlePeek);

                done.Wait(ALONGTIME_MS).Should().Be(true);
                running.Should().Be(0);
                peek.Should().Be(1);
            }
        }

        [Fact]
        public void ShouldScheduleParallelInParallel()
        {
            using (IScheduler s = new ProcessScheduler())
            {
                var done = new CountdownEvent(8); // 8x s.Add
                var running = 0;
                var peek = 0;

                Func<Task> HandlePeek = async () => {
                    var p = Interlocked.Increment(ref running);
                    lock (this) peek = Math.Max(peek, p);
                    await Task.Delay(SLEEPTIME_MS); // give a different HandlePeek task a chance to run
                    Interlocked.Decrement(ref running);
                    done.Signal();
                };

                s.Start();
                for (var i = 0; i<done.CurrentCount; i++)
                    s.Add(RequestProcessType.Parallel, HandlePeek);

                done.Wait(ALONGTIME_MS).Should().Be(true);
                running.Should().Be(0);
                peek.Should().BeGreaterThan(3);
            }
        }

        [Fact]
        public void ShouldScheduleMixed()
        {
            using (IScheduler s = new ProcessScheduler())
            {
                var done = new CountdownEvent(8); // 8x s.Add
                var running = 0;
                var peek = 0;

                Func<Task> HandlePeek = async () => {
                    var p = Interlocked.Increment(ref running);
                    lock (this) peek = Math.Max(peek, p);
                    await Task.Delay(SLEEPTIME_MS); // give a different HandlePeek task a chance to run
                    Interlocked.Decrement(ref running);
                    done.Signal();
                };

                s.Start();
                s.Add(RequestProcessType.Parallel, HandlePeek);
                s.Add(RequestProcessType.Parallel, HandlePeek);
                s.Add(RequestProcessType.Parallel, HandlePeek);
                s.Add(RequestProcessType.Parallel, HandlePeek);
                s.Add(RequestProcessType.Serial, HandlePeek);
                s.Add(RequestProcessType.Parallel, HandlePeek);
                s.Add(RequestProcessType.Parallel, HandlePeek);
                s.Add(RequestProcessType.Serial, HandlePeek);

                done.Wait(ALONGTIME_MS).Should().Be(true);
                running.Should().Be(0);
                peek.Should().BeGreaterThan(2);
            }
        }
    }
}