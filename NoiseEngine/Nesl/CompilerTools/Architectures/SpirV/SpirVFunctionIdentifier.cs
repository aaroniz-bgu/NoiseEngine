﻿using NoiseEngine.Nesl.CompilerTools.Architectures.SpirV.Types;
using System;
using System.Collections.Generic;

namespace NoiseEngine.Nesl.CompilerTools.Architectures.SpirV;

internal readonly struct SpirVFunctionIdentifier : IEquatable<SpirVFunctionIdentifier> {

    private readonly int hashCode;

    public NeslMethod NeslMethod { get; }
    public IReadOnlyList<SpirVVariable?> Parameters { get; }
    public int DynamicParameters { get; }

    public bool IsStatic => Parameters.Count == NeslMethod.ParameterTypes.Count;

    public SpirVFunctionIdentifier(NeslMethod neslMethod, ReadOnlySpan<SpirVVariable> parameters) {
        NeslMethod = neslMethod;

        SpirVVariable?[] p = new SpirVVariable?[parameters.Length];
        int dynamicParameters = 0;

        for (int i = 0; i < parameters.Length; i++) {
            if (!parameters[i].StorageClass.IsDynamicInLogicalAddressingModel())
                p[i] = parameters[i];
            else
                dynamicParameters++;
        }

        Parameters = p;
        DynamicParameters = dynamicParameters;

        // Compute hash code.
        HashCode hashCode = new HashCode();

        hashCode.Add(NeslMethod.Guid);
        foreach (SpirVVariable? parameter in Parameters)
            hashCode.Add(parameter);

        this.hashCode = hashCode.ToHashCode();
    }

    public bool Equals(SpirVFunctionIdentifier other) {
        if (NeslMethod != other.NeslMethod)
            return false;
        if (Parameters.Count != other.Parameters.Count)
            return false;

        for (int i = 0; i < Parameters.Count; i++) {
            if (Parameters[i] != other.Parameters[i])
                return false;
        }

        return true;
    }

    public override int GetHashCode() {
        return hashCode;
    }

    public override bool Equals(object? obj) {
        return obj is SpirVFunctionIdentifier identifier && Equals(identifier);
    }

}
