﻿/*
    Copyright (C) 2021 CodeStrikers.org
    This file is part of AgileDotNetSlayer.
    AgileDotNetSlayer is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    AgileDotNetSlayer is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with AgileDotNetSlayer.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Linq;
using AgileDotNetSlayer.Core.Interfaces;
using de4dot.blocks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace AgileDotNetSlayer.Core.Stages
{
    internal class DetectProtections : IStage
    {
        public void Run(IContext context)
        {
            try
            {
                if (CodeEncryption(context))
                    context.Logger.Warn(
                        "CODE encryption has been detected, incomplete deobfuscation of the assembly may result.");
            }
            catch { }

            try
            {
                if (CodeVirtualization(context))
                    context.Logger.Warn(
                        "CODE virtualization has been detected, incomplete deobfuscation of the assembly may result.");

            }
            catch { }
        }

        private static bool CodeVirtualization(IContext context)
            => DotNetUtils.GetResource(context.Module, "_CSVM") is EmbeddedResource;

        private static bool CodeEncryption(IContext context)
        {
            return context.Module.GetTypes()
                .Where(type =>
                    type.Fields.Count >= 1 && type.Fields.Any(x => x.FieldType.FullName == "System.Boolean") &&
                    type.Methods.Count(x => DotNetUtils.IsMethod(x, "System.Int32", "(System.IntPtr)")) == 4).Any(
                    type => type.Methods.Where(x => x.HasBody && x.Body.HasInstructions && DotNetUtils.IsMethod(x, "System.Void", "()")).Any(method =>
                        method.Body.Instructions.Any(x =>
                            x.OpCode.Equals(OpCodes.Callvirt) &&
                            x.Operand != null &&
                            x.Operand.ToString().Contains("System.Reflection.MethodBase::get_MethodHandle") &&
                            x.Operand.ToString().Contains("System.RuntimeMethodHandle"))));
        }
    }
}