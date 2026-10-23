using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Upcourt  {
    public class Input  {
        [StructLayout(LayoutKind.Sequential)]struct Pad {
            public ushort Buttons;
            public byte LT,RT;
            public short LX,LY,RX,RY;
        }
        [StructLayout(LayoutKind.Sequential)]struct State {
            public uint Packet;
            public Pad Pad;
        }
        [DllImport("xinput1_4.dll",EntryPoint="XInputGetState")]static extern uint Read4(uint i,out State s);
        [DllImport("xinput1_3.dll",EntryPoint="XInputGetState")]static extern uint Read3(uint i,out State s);
        [DllImport("xinput9_1_0.dll",EntryPoint="XInputGetState")]static extern uint Read9(uint i,out State s);
        public const int A=0x1000,B=0x2000,X=0x4000,Y=0x8000,LB=0x100,RB=0x200,Start=0x10,View=0x20,Up=1,Down=2,Left=4,Right=8;
        public HashSet<Keys> HeldKeys=new HashSet<Keys>();
        HashSet<Keys> prior=new HashSet<Keys>();
        int buttons,old,provider=4;
        public bool Connected,HadPad;public Settings Config;
        public int RawButtons{get{return buttons;}}public int PriorRawButtons{get{return old;}}
        public float MX,MY;
        public bool Sprint,UsesPad,Trick;
#if UPCOURT_TESTS
        public int? TestPadButtons;
#endif
        uint Read(uint index,out State state) {
            try {
                return provider==4?Read4(index,out state):provider==3?Read3(index,out state):Read9(index,out state);
            }
            catch(DllNotFoundException) {
                if(provider>3) {
                    provider=3;
                    return Read(index,out state);
                }
                if(provider==3) {
                    provider=1;
                    return Read(index,out state);
                }
                state=new State();
                return 1;
            }
            catch(EntryPointNotFoundException) {
                state=new State();
                return 1;
            }
        }
        public static float Axis(short value) {
            float v=value/32767f;
            return Math.Abs(v)<.2f?0:Math.Sign(v)*(Math.Abs(v)-.2f)/.8f;
        }
        public void Poll() {
            old=buttons;
            buttons=0;
            MX=MY=0;
            Connected=false;
            Sprint=HeldKeys.Contains(Keys.ShiftKey);Trick=false;
#if UPCOURT_TESTS
            if(TestPadButtons.HasValue){buttons=TestPadButtons.Value;Connected=true;return;}
#endif
            State s;
            for(uint i=0;i<4;i++)if(Read(i,out s)==0) {
                Connected=true;
                buttons=s.Pad.Buttons|(s.Pad.LT>100?0x10000:0)|(s.Pad.RT>100?0x20000:0);
                bool swap=Config!=null&&Config.SwapSticks;MX=Axis(swap?s.Pad.RX:s.Pad.LX);
                MY=-Axis(swap?s.Pad.RY:s.Pad.LY)*(Config!=null&&Config.InvertMoveY?-1:1);
                Sprint|=s.Pad.RT>100;Trick=s.Pad.LT>100;
                if(buttons!=old||Math.Abs(MX)+Math.Abs(MY)>.1f)UsesPad=true;
                break;
            }
        }
        public bool PadActionPress(Settings settings,int action){return ControlBindings.Active(settings,action,buttons)&&!ControlBindings.Active(settings,action,old);}
        public bool PadActionHeld(Settings settings,int action){return ControlBindings.Active(settings,action,buttons);}
        public bool PadActionRelease(Settings settings,int action){return ControlBindings.Matches(settings.PadBindings[action],old)&&!ControlBindings.Matches(settings.PadBindings[action],buttons);}
        public bool Key(Keys key) {
            return HeldKeys.Contains(key);
        }
        public bool Fresh(Keys key) {
            return Key(key)&&!prior.Contains(key);
        }
        public bool Released(Keys key) {
            return !Key(key)&&prior.Contains(key);
        }
        public bool Press(int flag) {
            return (buttons&flag)!=0&&(old&flag)==0;
        }
        public bool Release(int flag) {
            return (buttons&flag)==0&&(old&flag)!=0;
        }
        public bool Hold(int flag) {
            return (buttons&flag)!=0;
        }
        public Keys Binding(Settings settings,int action) {
            Keys key;
            return Enum.TryParse<Keys>(settings.Keys[action],out key)?key:Keys.None;
        }
        public bool Action(Settings s,int a) {
            return Key(Binding(s,a));
        }
        public bool ActionPress(Settings s,int a) {
            return Fresh(Binding(s,a));
        }
        public bool ActionRelease(Settings s,int a) {
            return Released(Binding(s,a));
        }
        public void EndFrame() {
            prior=new HashSet<Keys>(HeldKeys);
            HadPad=Connected;
        }
        public void Clear() {
            HeldKeys.Clear();
            prior.Clear();
            buttons=old=0;
            MX=MY=0;
        }
    }
}
