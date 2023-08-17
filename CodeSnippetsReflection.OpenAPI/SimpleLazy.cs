using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeSnippetsReflection.OpenAPI
{

    /*
     * A Simple lazy factory that substitues https://referencesource.microsoft.com/#mscorlib/system/Lazy.cs
     * by disregarding any exceptions thrown by the value factory.
     * This is a bypass for the dotnet issue https://github.com/dotnet/runtime/issues/27421 for lazy initialization without exception caching
     * 
     */
    public class SimpleLazy<T> where T : class
    {
        private readonly Func<T> valueFactory;
        private T instance;
        private readonly object locker = new object();

        public SimpleLazy(Func<T> valueFactory)
        {
            this.valueFactory = valueFactory ?? throw new ArgumentNullException("valueFactory");
            this.instance = null;
        }

        public T Value
        {
            get
            {
                if (instance != null)
                    return instance;// no need to acquire the lock if the instance is already initialized.
                
                lock (locker)
                    return instance ??= valueFactory();
            }
        }
    }
}
