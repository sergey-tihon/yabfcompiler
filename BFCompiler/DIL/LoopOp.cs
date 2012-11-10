﻿
namespace YABFcompiler.DIL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection.Emit;

    class LoopOp:DILInstruction
    {
        public DILOperationSet Instructions { get; set; }
        public List<LoopOp> NestedLoops { get;private set;}

        public LoopOp(Loop loop):this(new DILOperationSet(loop.Instructions))
        {
            
        }

        public LoopOp(DILOperationSet instructions)
        {
            Instructions = instructions;
            NestedLoops = Instructions.OfType<LoopOp>().ToList();
        }

        public LoopUnrollingResults Unroll()
        {
            var unrolled = new DILOperationSet();
            //if (IsClearanceLoop())
            //{
            //    unrolled.Add(new AssignOp(0, 0));
            //    return new LoopUnrollingResults(unrolled, true);
            //}

            var withUnrolledNestLoops = new DILOperationSet();
            foreach (var instruction in Instructions)
            {
                if (instruction is LoopOp)
                {
                    var nestedLoop = instruction as LoopOp;
                    var ur = nestedLoop.Unroll();
                    if (ur.WasLoopUnrolled)
                    {
                        withUnrolledNestLoops.AddRange(ur.UnrolledInstructions);
                    } else
                    {
                        withUnrolledNestLoops.Add(instruction);
                    }
                }
                else
                {
                    withUnrolledNestLoops.Add(instruction);
                }
            }

            if (IsSimple(withUnrolledNestLoops))
            {
                var walk = new CodeWalker().Walk(withUnrolledNestLoops);
                foreach (var cell in walk.Domain)
                {
                    if (cell.Key == 0)
                    {
                        continue;
                    }

                    unrolled.Add(new MultiplicationMemoryOp(cell.Key, cell.Value));
                }

                if (walk.Domain.ContainsKey(0))
                {
                    unrolled.Add(new AssignOp(0, 0));
                }

                return new LoopUnrollingResults(unrolled, true);
            }

            return new LoopUnrollingResults(withUnrolledNestLoops, false);
        }

        public static bool IsSimple(DILOperationSet operations)
        {
            // In here, I need to verify whether, ignoring nested
            return new CodeWalker().Walk(operations).EndPtrPosition == 0;
        }

        /// <summary>
        /// Returns true if a clearance pattern is detected with this loop
        /// 
        /// The following patterns are currently detected:
        ///     [-], [+]
        /// </summary>
        /// <returns></returns>
        public bool IsClearanceLoop()
        {
            if (Instructions.Count == 1)
            {
                if (Instructions[0].GetType() == typeof(AdditionMemoryOp))
                {
                    return true;
                }
            }

            return false;
        }
 
        public void Emit(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr)
        {
            var labels = EmitStartLoop(ilg);
            foreach (var instruction in Instructions)
            {
                instruction.Emit(ilg, array, ptr);
            }

            EmitEndLoop(ilg, array, ptr, labels.Item2, labels.Item1);
        }

        private Tuple<Label,Label> EmitStartLoop(ILGenerator ilg)
        {
            var L_0008 = ilg.DefineLabel();
            ilg.Emit(OpCodes.Br, L_0008);

            var L_0004 = ilg.DefineLabel();
            ilg.MarkLabel(L_0004);

            return new Tuple<Label, Label>(L_0008, L_0004);
        }

        private void EmitEndLoop(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr, Label go, Label mark)
        {
            ilg.MarkLabel(mark);
            ilg.Emit(OpCodes.Ldloc, array);
            ilg.Emit(OpCodes.Ldloc, ptr);
            ilg.Emit(OpCodes.Ldelem_U2);
            ilg.Emit(OpCodes.Brtrue, go);
        }
    }
}
