﻿namespace System
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;

#if !PCL
    static class ReflectionExtensions
    {
        public static IEnumerable<Type> TryGetExportedTypes(this Assembly assembly)
        {
            try
            {
                if (assembly.IsDynamic) return Enumerable.Empty<Type>();

                return assembly.ExportedTypes;
            }
            catch (ReflectionTypeLoadException e)
            {
                Debug.WriteLine("Error loading assembly: " + assembly.FullName);
                Debug.WriteLine(e.LoaderExceptions[0]);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error loading assembly: " + assembly.FullName);
                Debug.WriteLine(e);
            }
            return Enumerable.Empty<Type>();
        }

        public static IEnumerable<Assembly> LoadReferencedAssemblies(this Assembly assembly, Func<AssemblyName, bool> predicate = null, bool recursive = false)
        {
            var result = new HashSet<Assembly>();
            LoadReferencedAssemblies(assembly, predicate, recursive, result);
            return result;
        }

        private static void LoadReferencedAssemblies(Assembly assembly, Func<AssemblyName, bool> predicate, bool recursive, HashSet<Assembly> result)
        {
            if (assembly != null)
            {
                foreach (var name in assembly.GetReferencedAssemblies())
                {
                    if (predicate == null || predicate(name))
                    {
                        try
                        {
                            var loaded = Assembly.Load(name);

                            if (!result.Add(loaded))
                            {
                                continue;
                            }

                            if (recursive)
                            {
                                LoadReferencedAssemblies(loaded, predicate, recursive, result);
                            }
                        }
                        catch { }
                    }
                }
            }
        }
        
        public static Dictionary<Type, T> GroupByGenericTypeDefinition<T>(this IEnumerable<T> values, Type typeDefinition, bool throwOnDuplicate = true, int genericTypeIndex = 0)
        {
            var result = new Dictionary<Type, T>();
            if (values == null) return result;

            foreach (var value in values)
            {
                if (value == null) continue;

                var types =
                    from i in value.GetType().GetTypeInfo().ImplementedInterfaces
                    where i.GetTypeInfo().IsGenericType
                    let d = i.GetGenericTypeDefinition()
                    where d == typeDefinition
                    select i.GenericTypeArguments[genericTypeIndex];

                foreach (var type in types)
                {
                    T existing;
                    if (result.TryGetValue(type, out existing))
                    {
                        var error = type.FullName + " is implemented by both " + existing.GetType().FullName + " and " + value.GetType().FullName;
                        if (throwOnDuplicate) throw new InvalidOperationException(error);
                        Debug.WriteLine(error);
                    }
                    result[type] = value;
                }
            }
            return result;
        }
    }
#endif
}
