﻿
namespace YABFcompiler.DIL
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Reflection.Emit;

    [DebuggerDisplay("Write => Count: {Repeated}")]
    class WriteOp : IRepeatable, DILInstruction
    {
        public int Repeated { get; private set; }
        public ConstantValue Constant { get; private set; }

        private static readonly MethodInfo consoleWriteMethodInfo = typeof(Console).GetMethod("Write", new[] { typeof(char) });

        public WriteOp(int repeated): this(repeated, null)
        {
            
        }

        public WriteOp(int repeated, ConstantValue constant)
        {
            Constant = constant;
            Repeated = repeated;
        }

        public void Emit(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr)
        {
            for (int i = 0; i < Repeated; i++)
            {
                ilg.Emit(OpCodes.Ldloc, array);
                if (Constant != null)
                {
                    ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Constant.Value);
                }
                else
                {
                    ilg.Emit(OpCodes.Ldloc, ptr);
                }

                ilg.Emit(OpCodes.Ldelem_U2);
                ilg.EmitCall(OpCodes.Call, consoleWriteMethodInfo, null);
            }
        }

        public bool Repeat(DILOperationSet operations, int offset)
        {
            int repeated = Repeated, totalOperationsCovered = 1;
            for (int i = offset + 1; i < operations.Count; i++)
            {
                var instruction = operations[i] as WriteOp;
                if (instruction == null)
                {
                    break;
                }

                repeated += instruction.Repeated;
                totalOperationsCovered++;
            }

            if (totalOperationsCovered > 1)
            {
                operations.RemoveRange(offset, totalOperationsCovered);
                operations.Insert(offset, new WriteOp(repeated));
                return true;
            }

            return false;
        }
    }
}
