using Titled_Gui.Classes;
using Titled_Gui.Data.Game;
using static Titled_Gui.Data.Game.GameState;

namespace Titled_Gui.Modules.Legit
{
    public class Bhop : Classes.ThreadService // TODO make it use the jump action, i tried wouldnt work well
    {
        public static bool BhopEnable = false;
        public static float Chance = 100;
        public static int minDelay = 25;
        public static int maxDelay = 35;
        public static int HopKey = 0x20; // space
        private static Random random = new();
        private static int lastJumped = GlobalVar.GetTickCount();

        public static void AutoBhop()
        {
            if (User32.GetAsyncKeyState(HopKey) < 0)
            {
                for (int i = 0; i < 100; i++) // thanks stack overflow i dontg know how to do this outside unity
                {
                    int randomValueBetween0And99 = RandomGen.Next(100);
                    if (randomValueBetween0And99 < Chance)
                    {
                        if (Fflag == 65665 || Fflag == 65667)
                        {
                            GameState.swed.WriteInt(GameState.ForceJump, 65537); 
                            Thread.Sleep(5);
                        }
                        else
                        {
                            GameState.swed.WriteInt(GameState.ForceJump, 256);
                            Thread.Sleep(5); 
                        }

                    }
                }

            //var inputs = new User32.INPUT[2];
            //inputs[0].type = User32.INPUT_KEYBOARD;
            //inputs[0].U.ki.wVk = User32.VK_SPACE;
            //inputs[0].U.ki.wScan = 0x39;
            //inputs[1].U.ki.dwFlags = User32.KEYEVENTF_KEYDOWN;
            //inputs[0].U.ki.time = 0;
            //inputs[0].U.ki.dwExtraInfo = 0;
            //inputs[1] = inputs[0];
            //inputs[1].U.ki.dwFlags =User32.KEYEVENTF_KEYUP;
            ////Console.WriteLine(now);
            //User32.SendInput(2, inputs, Marshal.SizeOf<User32.INPUT>());
        }
        protected override void FrameAction()
        {
            if (!BhopEnable) return;
            GameState.Fflag = GameState.swed.ReadUInt(GameState.LocalPlayerPawn, Offsets.m_fFlags);
            AutoBhop();
        }
    }
}

