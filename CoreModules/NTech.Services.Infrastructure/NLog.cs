using System;
using System.Linq;
using System.Web;
using Serilog.Context;
using NTech.Services.Infrastructure;

namespace Serilog
{
    public static class NLog
    {
        private static void WithLogContext(Action a)
        {
            try
            {
                var context = HttpContext.Current?.GetOwinContext();
                if (context == null)
                {
                    a();
                }
                else
                {
                    using (LogContext.PushProperties(NTechLoggingMiddleware.GetProperties(context).ToArray()))
                    {
                        a();
                    }
                }
            }
            catch (InvalidOperationException)
            {
                a();
            }
        }
        public static void Debug<T>(string messageTemplate, T propertyValue)
        {
            WithLogContext(() =>
            {
                Log.Debug(messageTemplate, propertyValue);
            });
        }
        public static void Debug(string messageTemplate, params object[] propertyValues)
        {
            WithLogContext(() =>
            {
                Log.Debug(messageTemplate, propertyValues);
            });
        }      
        public static void Debug<T>(Exception exception, string messageTemplate, T propertyValue)
        {
            WithLogContext(() =>
            {
                Log.Debug(exception, messageTemplate, propertyValue);
            });
        }
        public static void Error(string message)
        {
            WithLogContext(() =>
            {
                Log.Error(message);
            });
        }
        public static void Error<T>(string messageTemplate, T propertyValue)
        {
            WithLogContext(() =>
            {
                Log.Error(messageTemplate, propertyValue);
            });
        }
        public static void Error(string messageTemplate, params object[] propertyValues)
        {
            WithLogContext(() =>
            {
                Log.Error(messageTemplate, propertyValues);
            });
        }
        public static void Error<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1)
        {
            WithLogContext(() =>
            {
                Log.Error(exception, messageTemplate, propertyValue0, propertyValue1);
            });
        }
        public static void Error(Exception exception, string message)
        {
            WithLogContext(() =>
            {
                Log.Error(exception, message);
            });
        }
        public static void Error<T>(Exception exception, string messageTemplate, T propertyValue)
        {
            WithLogContext(() =>
            {
                Log.Error(exception, messageTemplate, propertyValue);
            });
        }
        public static void Information(string message)
        {
            WithLogContext(() =>
            {
                Log.Information(message);
            });
        }
        public static void Information<T>(string messageTemplate, T propertyValue)
        {
            WithLogContext(() =>
            {
                Log.Information(messageTemplate, propertyValue);
            });
        }
        public static void Information(string messageTemplate, params object[] propertyValues)
        {
            WithLogContext(() =>
            {
                Log.Information(messageTemplate, propertyValues);
            });
        }
        public static void Warning(string message)
        {
            WithLogContext(() =>
            {
                Log.Warning(message);
            });
        }
        public static void Warning(Exception exception, string message)
        {
            WithLogContext(() =>
            {
                Log.Warning(exception, message);
            });
        }
        public static void Warning<T>(string messageTemplate, T propertyValue)
        {
            WithLogContext(() =>
            {
                Log.Warning(messageTemplate, propertyValue);
            });
        }
        public static void Warning<T>(Exception exception, string messageTemplate, T propertyValue)
        {
            WithLogContext(() =>
            {
                Log.Warning(exception, messageTemplate, propertyValue);
            });
        }
        public static void Warning<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1)
        {
            WithLogContext(() =>
            {
                Log.Warning(exception, messageTemplate, propertyValue0, propertyValue1);
            });
        }
        public static void Warning(string messageTemplate, params object[] propertyValues)
        {
            WithLogContext(() =>
            {
                Log.Warning(messageTemplate, propertyValues);
            });
        }
    }
}