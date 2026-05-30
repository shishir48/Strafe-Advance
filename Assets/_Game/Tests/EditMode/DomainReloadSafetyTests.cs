using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    /// <summary>
    /// Sprint 7.3 — domain-reload safety audit. With "Enter Play Mode without domain reload"
    /// enabled, static state survives between play sessions. Every singleton therefore needs a
    /// [RuntimeInitializeOnLoadMethod(SubsystemRegistration)] that re-nulls its static Instance,
    /// or the second play session boots holding a destroyed reference.
    ///
    /// This test reflects over the gameplay assembly and fails if any MonoBehaviour exposing a
    /// static `Instance` lacks such a reset hook — so a newly-added singleton can't silently
    /// regress the pattern.
    /// </summary>
    public class DomainReloadSafetyTests
    {
        [Test]
        public void EveryStaticInstanceSingleton_HasRuntimeInitializeReset()
        {
            Assembly gameAsm = typeof(GameManager).Assembly;

            var offenders = new List<string>();
            foreach (Type type in gameAsm.GetTypes())
            {
                if (!typeof(MonoBehaviour).IsAssignableFrom(type)) continue;

                PropertyInfo instanceProp = type.GetProperty(
                    "Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                if (instanceProp == null) continue; // not a singleton

                if (!HasRuntimeInitializeMethod(type))
                    offenders.Add(type.Name);
            }

            CollectionAssert.IsEmpty(offenders,
                "Singletons missing a [RuntimeInitializeOnLoadMethod] static reset (domain-reload unsafe):\n" +
                string.Join(", ", offenders));
        }

        static bool HasRuntimeInitializeMethod(Type type)
        {
            return type
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Any(m => m.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false).Length > 0);
        }
    }
}
