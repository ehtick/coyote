﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Rewriting.Types;
using Microsoft.Coyote.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;
using RuntimeCompiler = Microsoft.Coyote.Runtime.CompilerServices;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Rewriting pass for invocations between assemblies.
    /// </summary>
    internal class InterAssemblyInvocationRewritingPass : RewritingPass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InterAssemblyInvocationRewritingPass"/> class.
        /// </summary>
        internal InterAssemblyInvocationRewritingPass(IEnumerable<AssemblyInfo> visitedAssemblies, ILogger logger)
            : base(visitedAssemblies, logger)
        {
        }

        /// <inheritdoc/>
        protected override Instruction VisitInstruction(Instruction instruction)
        {
            if (this.Method is null)
            {
                return instruction;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                instruction.Operand is MethodReference methodReference &&
                this.IsForeignType(methodReference.DeclaringType))
            {
                TypeDefinition resolvedReturnType = methodReference.ReturnType.Resolve();
                if (IsTaskType(resolvedReturnType, NameCache.TaskName, NameCache.SystemTasksNamespace) ||
                    IsTaskType(resolvedReturnType, NameCache.ValueTaskName, NameCache.SystemTasksNamespace))
                {
                    string methodName = GetFullyQualifiedMethodName(methodReference);
                    Debug.WriteLine($"............. [+] injected returned uncontrolled task assertion for method '{methodName}'");

                    var providerType = this.Module.ImportReference(typeof(ExceptionProvider)).Resolve();
                    MethodReference providerMethod = providerType.Methods.FirstOrDefault(
                        m => m.Name is nameof(ExceptionProvider.ThrowIfReturnedTaskNotControlled));
                    providerMethod = this.Module.ImportReference(providerMethod);

                    this.Processor.InsertAfter(instruction, Instruction.Create(OpCodes.Call, providerMethod));
                    this.Processor.InsertAfter(instruction, Instruction.Create(OpCodes.Ldstr, methodName));
                    this.Processor.InsertAfter(instruction, Instruction.Create(OpCodes.Dup));
                    this.IsMethodBodyModified = true;
                }
                else if (methodReference.Name is "GetAwaiter" && IsTaskType(resolvedReturnType,
                    NameCache.TaskAwaiterName, NameCache.SystemCompilerNamespace))
                {
                    MethodReference wrapMethod = this.CreateTaskAwaiterWrapMethod(
                        typeof(RuntimeCompiler.TaskAwaiter), methodReference);
                    Instruction newInstruction = Instruction.Create(OpCodes.Call, wrapMethod);
                    Debug.WriteLine($"............. [+] {newInstruction}");

                    this.Processor.InsertAfter(instruction, newInstruction);
                    this.IsMethodBodyModified = true;
                }
                else if (methodReference.Name is "GetAwaiter" && IsTaskType(resolvedReturnType,
                    NameCache.ValueTaskAwaiterName, NameCache.SystemCompilerNamespace))
                {
                    MethodReference wrapMethod = this.CreateTaskAwaiterWrapMethod(
                        typeof(RuntimeCompiler.ValueTaskAwaiter), methodReference);
                    Instruction newInstruction = Instruction.Create(OpCodes.Call, wrapMethod);
                    Debug.WriteLine($"............. [+] {newInstruction}");

                    this.Processor.InsertAfter(instruction, newInstruction);
                    this.IsMethodBodyModified = true;
                }
            }

            return instruction;
        }

        /// <summary>
        /// Creates a wrap method of the specified task awaiter type.
        /// </summary>
        private MethodReference CreateTaskAwaiterWrapMethod(Type type, MethodReference methodReference)
        {
            var returnType = methodReference.ReturnType;
            TypeDefinition providerType = this.Module.ImportReference(type).Resolve();
            MethodReference wrapMethod = null;
            if (returnType is GenericInstanceType rgt)
            {
                TypeReference argType;
                if (methodReference.DeclaringType is GenericInstanceType dgt)
                {
                    var returnArgType = rgt.GenericArguments.FirstOrDefault().GetElementType();
                    argType = GetGenericParameterTypeFromNamedIndex(dgt, returnArgType.FullName);
                }
                else
                {
                    argType = rgt.GenericArguments.FirstOrDefault().GetElementType();
                }

                MethodDefinition genericMethod = providerType.Methods.FirstOrDefault(
                    m => m.Name == "Wrap" && m.HasGenericParameters);
                MethodReference wrapReference = this.Module.ImportReference(genericMethod);
                wrapMethod = MakeGenericMethod(wrapReference, argType);
            }
            else
            {
                wrapMethod = providerType.Methods.FirstOrDefault(
                m => m.Name is nameof(RuntimeCompiler.TaskAwaiter.Wrap));
            }

            return this.Module.ImportReference(wrapMethod);
        }

        /// <summary>
        /// Checks if the specified type is the expected task type.
        /// </summary>
        private static bool IsTaskType(TypeDefinition type, string expectedName, string expectedNamespace)
        {
            if (type != null)
            {
                if (IsSystemType(type) && type.Namespace == expectedNamespace &&
                    (type.Name == expectedName || type.Name.StartsWith(expectedName + "`")))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the specified type is a task-like type.
        /// </summary>
        private static bool IsTaskLikeType(TypeDefinition type)
        {
            if (type is null)
            {
                return false;
            }

            var interfaceTypes = type.Interfaces.Select(i => i.InterfaceType);
            if (!interfaceTypes.Any(
                i => i.FullName is "System.Runtime.CompilerServices.INotifyCompletion" ||
                i.FullName is "System.Runtime.CompilerServices.INotifyCompletion"))
            {
                return false;
            }

            if (type.Methods.Any(m => m.Name is "get_IsCompleted"))
            {
                return true;
            }

            return false;
        }
    }
}