﻿using System.Collections.Generic;
using System.Linq;

namespace NoiseEngine.Nesl;

public abstract class NeslAssembly {

    public abstract IEnumerable<NeslType> Types { get; }

    public string Name { get; }

    protected NeslAssembly(string name) {
        Name = name;
    }

    /// <summary>
    /// Finds <see cref="NeslType"/> with given <paramref name="fullName"/>
    /// in this <see cref="NeslAssembly"/> and their dependencies.
    /// </summary>
    /// <param name="fullName">Full name of the searched <see cref="NeslType"/>.</param>
    /// <returns><see cref="NeslType"/> when type was found, <see langword="null"/> when not.</returns>
    public NeslType? GetType(string fullName) {
        return Types.FirstOrDefault(x => x.FullName == fullName);
    }

    internal abstract NeslType GetType(ulong localTypeId);
    internal abstract NeslMethod GetMethod(ulong localMethodId);

}
