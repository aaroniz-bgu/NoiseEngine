﻿using NoiseEngine.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NoiseEngine.Jobs2.Commands;

internal class SystemCommandsExecutor {

    private readonly FastList<SystemCommand> commands;
    private readonly Dictionary<Type, (IComponent? value, int size)> components =
        new Dictionary<Type, (IComponent? value, int size)>();

    private int index;
    private int indexEntity;
    private EntityCommandsInner? entityCommands;
    private bool writeAccess;

    public SystemCommandsExecutor(FastList<SystemCommand> commands) {
        this.commands = commands;
    }

    public void Invoke() {
        while (index < commands.Count) {
            SystemCommand command = commands[index];
            switch (command.Type) {
                case SystemCommandType.GetEntity:
                    ProcessEntity();
                    entityCommands = (EntityCommandsInner)command.Value!;
                    indexEntity = index;
                    break;
                case SystemCommandType.EntityInsert:
                    writeAccess = true;
                    (IComponent component, int size) value = ((IComponent, int))command.Value!;
                    Type type = value.component.GetType();
                    components[type] = value;
                    break;
                case SystemCommandType.EntityRemove:
                    Type typeB = (Type)command.Value!;
                    writeAccess |= entityCommands!.Entity.Contains(typeB);
                    components[typeB] = (null, 0);
                    break;
                default:
                    throw new UnreachableException();
            }
            index++;
        }

        ProcessEntity();
    }

    private void ProcessEntity() {
        if (entityCommands is null)
            return;

        Entity entity = entityCommands.Entity;
        EntityLockerHeld held;
        if (entityCommands.ConditionalsCount > 0) {
            (Entity, bool)[] entities = new (Entity, bool)[entityCommands.ConditionalsCount + 1];
            entities[0] = (entity, writeAccess);

            for (int i = 0; i < entityCommands.ConditionalsCount; i++)
                entities[i + 1] = (entityCommands.Conditionals[i].Entity, false);

            if (!EntityLocker.TryLockEntities(entities, out held))
                return;
        } else if (!EntityLocker.TryLockEntity(entity, writeAccess, out held)) {
            return;
        }

        if (!writeAccess) {
            foreach ((Type type, (IComponent? value, _)) in components) {
                if ((value is null && entityCommands.Entity.Contains(type)) || !entityCommands.Entity.Contains(type)) {
                    held.Dispose();
                    writeAccess = true;
                    ProcessEntity();
                    return;
                }
            }
        }

        if (entityCommands.ConditionalsCount == 0) {
            ArchetypeChunk oldChunk = entity.chunk!;
            (ArchetypeChunk newChunk, nint newIndex) = entity.World.GetArchetype(
                entity.chunk!.Archetype.ComponentTypes.Select(x => x.type).Where(
                    x => !components.TryGetValue(x, out (IComponent? value, int size) o) || o.value is not null
                ).Union(components.Where(x => x.Value.value is not null).Select(x => x.Key)),
                () => entity.chunk!.Archetype.ComponentTypes.Where(
                    x => !components.TryGetValue(x.type, out (IComponent? value, int size) o) || o.value is not null
                ).Union(components.Where(x => x.Value.value is not null).Select(x => (x.Key, x.Value.size))).ToArray()
            ).TakeRecord();

            entity.chunk = newChunk;
            nint oldIndex = entity.index;
            entity.index = newIndex;

            unsafe {
                fixed (byte* dp = newChunk.StorageData) {
                    byte* di = dp + newIndex;
                    fixed (byte* sp = oldChunk.StorageData) {
                        byte* si = sp + oldIndex;

                        // Copy old components.
                        foreach ((Type type, int size) in newChunk.Archetype.ComponentTypes) {
                            (IComponent? value, int size) component;
                            if (oldChunk.Offsets.TryGetValue(type, out nint oldOffset)) {
                                if (!components.TryGetValue(type, out component)) {
                                    Buffer.MemoryCopy(si + oldOffset, di + newChunk.Offsets[type], size, size);
                                    continue;
                                }
                            } else {
                                component = components[type];
                            }

                            fixed (byte* vp = &Unsafe.As<IComponent, byte>(ref component.value!)) {
                                Buffer.MemoryCopy(
                                    (void*)(Unsafe.Read<IntPtr>(vp) + sizeof(nint)),
                                    di + newChunk.Offsets[type], size, size
                                );
                            }
                        }

                        // Copy internal component.
                        int iSize = Unsafe.SizeOf<EntityInternalComponent>();
                        Buffer.MemoryCopy(si, di, iSize, iSize);

                        // Clear old data.
                        new Span<byte>(si, (int)oldChunk.Archetype.RecordSize).Clear();
                    }
                }
            }

            held.Dispose();
            oldChunk.Archetype.ReleaseRecord(oldChunk, oldIndex);
            return;
        }

        held.Dispose();
    }

}
