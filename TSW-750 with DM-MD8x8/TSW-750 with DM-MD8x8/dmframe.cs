using Crestron.SimplSharp;                              // For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                           // For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.DM;
using Crestron.SimplSharpPro.DM.Cards;


namespace TSW750_with_DMMD8x8
{

    public class DMFrameInit
    {

        private DmMd8x8 _frame;
        private DmcC input1;
        private DmcDvi input2;
        private DmcHd input3;
        private DmcS input4;
        private DmcC input5;
        private Dmc4kHd input6;
        private DmcVga input7;
        private DmcStr input8;

        private Dmc4kCoHdSingle output1_2;
        private Dmc4kHdoSingle output3_4;
        private DmcStroSingle output5_6;
        private DmcCoHdSingle output7_8;

        //public CrestronCollection<ICardInputOutputType> myICards;
        //public CrestronCollection<ICardInputOutputType> myOCards;

        public DMFrameInit()
        {
        }

        public DmMd8x8 DMFrame(uint paramIpId, CrestronControlSystem paramControlSystem)
        {

            _frame = new DmMd8x8(paramIpId, paramControlSystem);

            ErrorLog.Notice("TDS: this is inside the frame");

            input1 = new DmcC(1, _frame);
            input2 = new DmcDvi(2, _frame);
            input3 = new DmcHd(3, _frame);
            input4 = new DmcS(4, _frame);
            input5 = new DmcC(5, _frame);
            input6 = new Dmc4kHd(6, _frame);
            input7 = new DmcVga(7, _frame);
            input8 = new DmcStr(8, _frame);
            output1_2 = new Dmc4kCoHdSingle(1, _frame);
            output3_4 = new Dmc4kHdoSingle(2, _frame);
            output5_6 = new DmcStroSingle(3, _frame);
            output7_8 = new DmcCoHdSingle(4, _frame);

            _frame.VideoEnter.BoolValue = true;

            return _frame;
        }

        public delegate void DMSwitchEvent(DMOutput output, DMInput input);
        public event DMSwitchEvent dmEventthing;
        

        /*
         * DMPS 2 or 3 series integrated processor switcher configurations
         * 
        public void CardGetter()
        {
            myICards = new CrestronCollection<ICardInputOutputType>();
            myOCards = new CrestronCollection<ICardInputOutputType>();

            uint x = 0;
            uint y = 0;

            foreach (var card in _frame.Inputs)
            {
                myICards.TryGetValue(x, out ICardInputOutputType thiscard);
                ErrorLog.Notice("TDS: current card is = {0} and {1}", card.Name);
                x++;
            }
            foreach (var card in _frame.Outputs)
            {
                myOCards.TryGetValue(y, out ICardInputOutputType thiscard);
                ErrorLog.Notice("TDS: current card is = {0} and {1}", card.Name);
                y++;
            }
        }
        */
    }
}