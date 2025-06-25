using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web.Hosting;
using Serilog;

namespace NTech.Services.Infrastructure.Eventing
{
    public class NTechEvent
    {
        public string EventTypeName { get; set; }
        public string EventData { get; set; }
    }

    public class NTechEventHandler
    {
        private class EventSubscriber
        {
            public string SubscriberId { get; set; }
            public string EventTypeName { get; set; }
            public Action<string, CancellationToken> OnEvent { get; set; } //eventdata, cancellation token
        }

        private readonly ConcurrentQueue<NTechEvent> eventQueue = new ConcurrentQueue<NTechEvent>();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, EventSubscriber>>
            subscribersByEventTypeName =
                new ConcurrentDictionary<string, ConcurrentDictionary<string, EventSubscriber>>(StringComparer
                    .InvariantCultureIgnoreCase);

        private readonly ConcurrentDictionary<string, EventSubscriber> subscribersBySubscriberId =
            new ConcurrentDictionary<string, EventSubscriber>();

        private readonly AutoResetEvent newEventSignal = new AutoResetEvent(false);

        private static NTechEventHandler _instance;

        private NTechEventHandler()
        {
        }

        public static void PublishEvent(string eventTypeName, string eventData)
        {
            if (_instance == null)
                return;

            _instance.eventQueue.Enqueue(new NTechEvent { EventTypeName = eventTypeName, EventData = eventData });
            _instance.newEventSignal.Set();
        }

        private string Subscribe(string eventTypeName, Action<string, CancellationToken> onEvent)
        {
            var subs = subscribersByEventTypeName.GetOrAdd(eventTypeName,
                _ => new ConcurrentDictionary<string, EventSubscriber>());
            var subscriberId = Guid.NewGuid().ToString();

            var evt = new EventSubscriber
            {
                EventTypeName = eventTypeName,
                OnEvent = onEvent,
                SubscriberId = subscriberId
            };

            subs.TryAdd(evt.SubscriberId, evt);
            subscribersBySubscriberId.TryAdd(evt.SubscriberId, evt);

            return subscriberId;
        }

        /// <summary>
        /// Loads event subscribers locally and from plugins
        /// </summary>
        /// <param name="globalAsaxAssembly">typeof(Global) or some other way to get the assembly that contains the actual service.</param>
        /// <param name="enabledPluginNames">List of plugins (dll names like test.foo.dll) to load</param>
        /// <param name="additionalPluginFolders">Additional paths to look for plugins</param>
        /// <param name="assemblyLoader">Optional assembly loader to use</param>
        /// <returns></returns>
        public static NTechEventHandler CreateAndLoadSubscribers(Assembly globalAsaxAssembly,
            List<string> enabledPluginNames, List<string> additionalPluginFolders = null,
            NTechExternalAssemblyLoader assemblyLoader = null)
        {
            _instance = new NTechEventHandler();

            LoadSubscribers(globalAsaxAssembly);

            if (enabledPluginNames != null && enabledPluginNames.Any())
            {
                if (assemblyLoader == null)
                    assemblyLoader = new NTechExternalAssemblyLoader();

                var pluginFolders = new List<string>();

                if (additionalPluginFolders != null)
                    pluginFolders.AddRange(additionalPluginFolders);

                //Load this last since this this has had wierd behaviour with which path it finds in the past. This way it can be overcome in production without changing code
                //by poiting an additional plugin folder to the bin folder of the service which will then be searched first.
                pluginFolders.Add(Path.GetDirectoryName(new Uri(globalAsaxAssembly.CodeBase).LocalPath));

                var loadedPluginNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

                foreach (var p in assemblyLoader.LoadPlugins(pluginFolders, enabledPluginNames))
                {
                    Log.Information("Plugin '{PluginName}' loading from '{PluginFolder}'", p.Item1.Name,
                        p.Item1.Directory.FullName);
                    LoadSubscribers(p.Item2);
                    loadedPluginNames.Add(p.Item1.Name);
                }

                var notLoadedPluginNames =
                    new HashSet<string>(enabledPluginNames, StringComparer.InvariantCultureIgnoreCase)
                        .Except(loadedPluginNames)
                        .ToHashSet();

                if (notLoadedPluginNames.Any())
                {
                    throw new Exception(
                        $"The follow enabled plugins were not loaded so the module will not start: {string.Join(",", notLoadedPluginNames)}");
                }
            }

            HostingEnvironment.QueueBackgroundWorkItem(t => _instance.ProcessQueue(t));

            return _instance;

            void LoadSubscribers(Assembly a)
            {
                var types = a.GetTypes();
                foreach (var type in types)
                {
                    if (type.GetInterface("NTech.Services.Infrastructure.Eventing.IEventSubscriber") == null) continue;
                    var sub = (IEventSubscriber)type.GetConstructor(Type.EmptyTypes)?.Invoke(null);
                    if (sub == null)
                        throw new Exception($"Event subscriber {type.Name} does not have an empty constructor");
                    sub.OnStartup(_instance.Subscribe);
                    Log.Information("Event subscriber registered: {SubscriberName}", type.FullName);
                }
            }
        }

        private void ProcessQueue(CancellationToken t)
        {
            var processedCount = 0;
            var w = Stopwatch.StartNew();
            while (eventQueue.TryDequeue(out var evt))
            {
                if (subscribersByEventTypeName.TryGetValue(evt.EventTypeName, out var subs))
                {
                    foreach (var sub in subs.Values.ToList())
                    {
                        try
                        {
                            sub?.OnEvent(evt.EventData, t);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error processing event of type '{EventType}' in queue", evt?.EventTypeName);
                        }

                        if (t.IsCancellationRequested)
                            break;
                    }
                }

                processedCount++;
                if (t.IsCancellationRequested)
                    break;
            }

            w.Stop();
            Log.Information("Event queue processed {ProcessedCount} events in {EventTime}ms", processedCount,
                w.ElapsedMilliseconds);
            WaitHandle.WaitAny(new[] { newEventSignal, t.WaitHandle });

            if (t.IsCancellationRequested)
                return;

            HostingEnvironment.QueueBackgroundWorkItem(ProcessQueue);
        }
    }
}