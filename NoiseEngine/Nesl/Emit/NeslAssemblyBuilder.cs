﻿using NoiseEngine.Nesl.CompilerTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace NoiseEngine.Nesl.Emit;

public class NeslAssemblyBuilder : NeslAssembly {

    private readonly ConcurrentDictionary<NeslMethod, LocalMethodId> localMethodIds =
        new ConcurrentDictionary<NeslMethod, LocalMethodId>();

    private readonly ConcurrentDictionary<string, NeslTypeBuilder> types =
        new ConcurrentDictionary<string, NeslTypeBuilder>();

    private ulong latestLocalMethodId;

    public override IEnumerable<NeslType> Types => types.Values;

    private NeslAssemblyBuilder(string name) : base(name) {
    }

    /// <summary>
    /// Creates new <see cref="NeslAssemblyBuilder"/>.
    /// </summary>
    /// <param name="name">Name of new <see cref="NeslAssemblyBuilder"/>.</param>
    /// <returns>New <see cref="NeslAssemblyBuilder"/>.</returns>
    public static NeslAssemblyBuilder DefineAssembly(string name) {
        return new NeslAssemblyBuilder(name);
    }

    /// <summary>
    /// Creates new <see cref="NeslTypeBuilder"/> in this assembly.
    /// </summary>
    /// <param name="fullName">Name preceded by namespace.</param>
    /// <param name="attributes"><see cref="TypeAttributes"/> of new type.</param>
    /// <returns>New <see cref="NeslTypeBuilder"/>.</returns>
    /// <exception cref="ArgumentException">
    /// <see cref="NeslType"/> with this <paramref name="fullName"/> already exists in this assembly.
    /// </exception>
    public NeslTypeBuilder DefineType(
        string fullName, TypeAttributes attributes = TypeAttributes.Public | TypeAttributes.Class
    ) {
        NeslTypeBuilder type = new NeslTypeBuilder(this, fullName, attributes);

        if (!types.TryAdd(fullName, type)) {
            throw new ArgumentException($"{nameof(NeslType)} named `{fullName}` already exists in `{Name}` assembly.",
                nameof(fullName));
        }

        return type;
    }

    internal LocalMethodId GetLocalMethodId(NeslMethod method) {
        return localMethodIds.GetOrAdd(method, _ => new LocalMethodId(Interlocked.Increment(ref latestLocalMethodId)));
    }

}
