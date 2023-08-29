using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace YarpAuthProxy.BffLibrary;

public static class SessionUtils
{

    public static void SetObject<T>(this Microsoft.AspNetCore.Http.ISession session, string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        session.SetString(key, json);
    }

    public static T? GetObject<T>(this Microsoft.AspNetCore.Http.ISession session, string key)
    {
        if (!session.Keys.Contains(key))
        {
            return default(T);
        }

        var value = session.GetString(key);

        if (string.IsNullOrEmpty(value))
        {
            return default(T);
        }

        return JsonSerializer.Deserialize<T>(value);
    }

}