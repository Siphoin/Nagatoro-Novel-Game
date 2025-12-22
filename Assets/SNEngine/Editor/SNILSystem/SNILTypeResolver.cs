using System;
using System.Linq;
using SiphoinUnityHelpers.XNodeExtensions;

namespace SNEngine.Editor.SNILSystem
{
    public class SNILTypeResolver
    {
        public static Type GetNodeType(string name)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var type = assembly.GetTypes().FirstOrDefault(t =>
                        t.Name == name && typeof(BaseNode).IsAssignableFrom(t));
                    if (type != null) return type;
                }
                catch { continue; }
            }

            return null;
        }
    }
}