//
// Copyright (c) 2019-2020 Ryujinx
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
//

using Ryujinx.Audio.Renderer.Common;
using Ryujinx.Audio.Renderer.Dsp.State;
using Ryujinx.Audio.Renderer.Parameter;
using Ryujinx.Audio.Renderer.Parameter.Effect;
using Ryujinx.Audio.Renderer.Server.MemoryPool;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DspAddress = System.UInt64;

namespace Ryujinx.Audio.Renderer.Server.Effect
{
    /// <summary>
    /// Server state for a delay effect.
    /// </summary>
    public class DelayEffect : BaseEffect
    {
        /// <summary>
        /// The delay parameter.
        /// </summary>
        public DelayParameter Parameter;

        /// <summary>
        /// The delay state.
        /// </summary>
        public Memory<DelayState> State { get; }

        public DelayEffect()
        {
            State = new DelayState[1];
        }

        public override EffectType TargetEffectType => EffectType.Delay;

        public override DspAddress GetWorkBuffer(int index)
        {
            return GetSingleBuffer();
        }

        public override void Update(out BehaviourParameter.ErrorInfo updateErrorInfo, ref EffectInParameter parameter, PoolMapper mapper)
        {
            Debug.Assert(IsTypeValid(ref parameter));

            ref DelayParameter delayParameter = ref MemoryMarshal.Cast<byte, DelayParameter>(parameter.SpecificData)[0];

            updateErrorInfo = new BehaviourParameter.ErrorInfo();

            if (delayParameter.IsChannelCountMaxValid())
            {
                UpdateParameterBase(ref parameter);

                UsageState oldParameterStatus = Parameter.Status;

                Parameter = delayParameter;

                if (delayParameter.IsChannelCountValid())
                {
                    IsEnabled = parameter.IsEnabled;

                    if (oldParameterStatus != UsageState.Enabled)
                    {
                        Parameter.Status = oldParameterStatus;
                    }

                    if (BufferUnmapped || parameter.IsNew)
                    {
                        UsageState = UsageState.New;
                        Parameter.Status = UsageState.Invalid;

                        BufferUnmapped = !mapper.TryAttachBuffer(out updateErrorInfo, ref WorkBuffers[0], parameter.BufferBase, parameter.BufferSize);
                    }
                }
            }
        }

        public override void UpdateForCommandGeneration()
        {
            UpdateUsageStateForCommandGeneration();

            Parameter.Status = UsageState.Enabled;
        }
    }
}
