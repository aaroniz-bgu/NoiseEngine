﻿using NoiseEngine.Nesl.CompilerTools.Architectures.SpirV.Types;
using NoiseEngine.Nesl.Emit.Attributes.Internal;
using System;
using System.Collections.Generic;

namespace NoiseEngine.Nesl.CompilerTools.Architectures.SpirV.Intrinsics;

internal static class IntrinsicsManager {

    private const string DefaultAssembly = "System";

    public static void Process(
        SpirVCompiler compiler, NeslMethod neslMethod, SpirVGenerator generator, IReadOnlyList<SpirVVariable> parameters
    ) {
        if (neslMethod.Assembly.Name != DefaultAssembly) {
            throw new InvalidOperationException(
                $"{nameof(IntrinsicAttribute)} can only be used in {DefaultAssembly} assembly."
            );
        }

        switch (neslMethod.Type.FullName) {
            // TODO: Remove this. This temporarily shows how to use it.
            // case $"{DefaultAssembly}.{nameof(ReadWriteBuffer)}`1":
            //     new ReadWriteBuffer(compiler,neslMethod, generator, parameters).Process();
            //     break;
            default:
                throw new InvalidOperationException($"Unable to find given {nameof(IntrinsicAttribute)} definition.");
        }
    }

}
