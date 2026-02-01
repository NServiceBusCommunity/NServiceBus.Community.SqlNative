static class DelegateWrappers
{
    static string threwAnException = "Provided {0} delegate threw an exception.";
    static string returnedNull = "Provided {0} delegate returned a null.";

    public static Action<T> WrapFunc<T>(this Action<T> func, string name) =>
        _ =>
        {
            try
            {
                func(_);
            }
            catch (Exception exception)
            {
                var message = string.Format(threwAnException, name);
                throw new(message, exception);
            }
        };

    static void ThrowIfNull(string name, object? value)
    {
        if (value == null)
        {
            var nullMessage = string.Format(returnedNull, name);
            throw new(nullMessage);
        }
    }

    public static Func<T1, T2, T3, Task> WrapFunc<T1, T2, T3>(this Func<T1, T2, T3, Task> func, string name) =>
        async (x, y, z) =>
        {
            Task task;
            try
            {
                task = func(x, y, z);
            }
            catch (Exception exception)
            {
                var message = string.Format(threwAnException, name);
                throw new(message, exception);
            }

            ThrowIfNull(name, task);

            try
            {
                await task;
            }
            catch (Exception exception)
            {
                var message = string.Format(threwAnException, name);
                throw new(message, exception);
            }
        };

    public static Func<T1,T2, Task<K>> WrapFunc<T1, T2, K>(this Func<T1, T2, Task<K>> func, string name) =>
        async (x,y) =>
        {
            Task<K> task;
            try
            {
                task = func(x,y);
            }
            catch (Exception exception)
            {
                var message = string.Format(threwAnException, name);
                throw new(message, exception);
            }

            ThrowIfNull(name, task);

            try
            {
                return await task;
            }
            catch (Exception exception)
            {
                var message = string.Format(threwAnException, name);
                throw new(message, exception);
            }
        };

    public static Func<T, Task<K>> WrapFunc<T, K>(this Func<T, Task<K>> func, string name) =>
        async _ =>
        {
            Task<K> task;
            try
            {
                task = func(_);
            }
            catch (Exception exception)
            {
                var message = string.Format(threwAnException, name);
                throw new(message, exception);
            }

            ThrowIfNull(name, task);

            try
            {
                return await task;
            }
            catch (Exception exception)
            {
                var message = string.Format(threwAnException, name);
                throw new(message, exception);
            }
        };
}