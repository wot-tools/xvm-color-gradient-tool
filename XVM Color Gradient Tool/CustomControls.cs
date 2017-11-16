using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Globalization;
using System.Windows.Threading;

namespace XVMCGT
{
    [TemplatePart(Name = "RBT_Increase", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "RBT_Decrease", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "TB_Value", Type = typeof(TextBox))]
    [TemplatePart(Name = "TB_Suffix", Type = typeof(TextBlock))]
    [TemplatePart(Name = "ArrowUp", Type = typeof(Path))]
    [TemplatePart(Name = "ArrowDown", Type = typeof(Path))]
    public class Custom_DoubleUpDown : Control
    {
        public static readonly DependencyProperty ValueProperty;
        public static readonly DependencyProperty DefaultValueProperty;
        public static readonly DependencyProperty IntervalProperty;
        public static readonly DependencyProperty MaximumProperty;
        public static readonly DependencyProperty MinimumProperty;
        public static readonly DependencyProperty SuffixProperty;
        public static readonly DependencyProperty TrailingDigitsProperty;
        public static readonly DependencyProperty DelayedEventStartCountProperty;

        public static readonly RoutedEvent ValueChangedEvent;

        private RepeatButton RBT_Increase;
        private RepeatButton RBT_Decrease;
        private TextBox TB_Value;
        private TextBlock TB_Suffix;
        private DispatcherTimer timer;

        private double eventcount = 0;
        private bool ignoretextchanggeflag = false;

        static Custom_DoubleUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Custom_DoubleUpDown), new FrameworkPropertyMetadata(typeof(Custom_DoubleUpDown)));

            ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(Custom_DoubleUpDown), new PropertyMetadata(0.0));
            DefaultValueProperty = DependencyProperty.Register("DefaultValue", typeof(double), typeof(Custom_DoubleUpDown), new PropertyMetadata(0.0));
            IntervalProperty = DependencyProperty.Register("Interval", typeof(double), typeof(Custom_DoubleUpDown), new PropertyMetadata(0.1));
            MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), typeof(Custom_DoubleUpDown), new PropertyMetadata(double.MaxValue));
            MinimumProperty = DependencyProperty.Register("Minimum", typeof(double), typeof(Custom_DoubleUpDown), new PropertyMetadata(double.MinValue));
            SuffixProperty = DependencyProperty.Register("Suffix", typeof(string), typeof(Custom_DoubleUpDown), new PropertyMetadata(""));
            TrailingDigitsProperty = DependencyProperty.Register("TrailingDigits", typeof(int), typeof(Custom_DoubleUpDown), new PropertyMetadata(2));
            DelayedEventStartCountProperty = DependencyProperty.Register("DelayedEventStartCount", typeof(int), typeof(Custom_DoubleUpDown), new PropertyMetadata(1));

            ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Custom_DoubleUpDown));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            RBT_Increase = this.GetTemplateChild("RBT_Increase") as RepeatButton;
            RBT_Increase.Click += new RoutedEventHandler(RBT_Increase_Click);
            RBT_Decrease = this.GetTemplateChild("RBT_Decrease") as RepeatButton;
            RBT_Decrease.Click += new RoutedEventHandler(RBT_Decrease_Click);

            TB_Value = this.GetTemplateChild("TB_Value") as TextBox;
            TB_Value.TextChanged += new TextChangedEventHandler(TB_Value_TextChanged);
            //TB_Value.MouseWheel += new MouseWheelEventHandler(TB_Value_MouseWheel);

            this.MouseWheel += new MouseWheelEventHandler(CustomUpDown_MouseWheel);

            TB_Suffix = this.GetTemplateChild("TB_Suffix") as TextBlock;

            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            timer.Tick += new EventHandler(timer_Tick);

            ManipulateValue(0);
        }

        #region Events
        public event RoutedEventHandler ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); } 
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        void RaiseValueChangedEvent()
        {
            if (eventcount >= DelayedEventStartCount)
            {
                RoutedEventArgs newEventArgs = new RoutedEventArgs(Custom_DoubleUpDown.ValueChangedEvent,eventcount);
                RaiseEvent(newEventArgs);
            }

            eventcount++;
        }

        void TB_Value_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!ignoretextchanggeflag)
            {
                timer.Stop();
                timer.Start();
            }
        }

        void CustomUpDown_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (this.IsMouseOver)
            {
                if (e.Delta > 0)
                {
                    ManipulateValue(Interval);
                }
                else
                {
                    ManipulateValue(-Interval);
                }

                e.Handled = true;
            }
        }

        void TB_Value_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (TB_Value.IsFocused)
            {
                if (e.Delta > 0)
                {
                    ManipulateValue(Interval);
                }
                else
                {
                    ManipulateValue(-Interval);
                }

                e.Handled = true;
            }
        }

        void RBT_Increase_Click(object sender, RoutedEventArgs e)
        {
            ManipulateValue(Interval);
        }
        void RBT_Decrease_Click(object sender, RoutedEventArgs e)
        {
            ManipulateValue(-Interval);
        }

        #endregion

        void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            UpdateValueFromTextbox();
        }


        #region Fix Textbox Value if changed by user
       
        void UpdateValueFromTextbox()
        {
            List<Char> chars = TB_Value.Text.ToList<Char>();
            if (chars.Count > 0)
            {
                int i = 0;

                if (chars[0] == '-')
                    i = 1;


                bool kommaread = false;
                while (i < chars.Count)
                {
                    Char c = chars[i];
                    if (!Char.IsDigit(c))
                    {
                        if (Char.IsPunctuation(c) && !kommaread &&!c.Equals('-'))
                        {
                            kommaread = true;
                            i++;
                        }
                        else
                        {
                            chars.RemoveAt(i);
                        }
                    }
                    else
                    {
                        i++;
                    }

                }
                string s_chars = string.Join("", chars.ToArray<char>());
                try
                {
                    double val = Convert.ToDouble(s_chars);
                    val = Math.Round(val, TrailingDigits);
                    ManipulateValue(val - Value);
                }
                catch
                {
                    ManipulateValue(DefaultValue - Value);
                }
            }
            else
            {
                ManipulateValue(DefaultValue - Value);
            }
        }
        #endregion

        void ManipulateValue(double Amount)
        {
            Value += Amount;

            if (Value <= Minimum)
            {
                Value = Minimum;
            }

            if (Value >= Maximum)
            {
                Value = Maximum;
            }

            ChangeTextboxText(Value.ToString("N" + TrailingDigits.ToString()));

            if (IsEnabled)
            {
                RBT_Decrease.IsEnabled = true;
                RBT_Increase.IsEnabled = true;

                if (Value == Maximum)
                {
                    RBT_Increase.IsEnabled = false;
                }

                if (Value == Minimum)
                {
                    RBT_Decrease.IsEnabled = false;
                }
            }
            else
            {

            }

            RaiseValueChangedEvent();
        }

        void ChangeTextboxText(string text)
        {
            ignoretextchanggeflag = true;
            TB_Value.Text = text;
            ignoretextchanggeflag = false;
        }

        #region Properties
        [Description("Value"), Category("Common Properties")]
        public double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        [Description("DefaultValue"), Category("Common Properties")]
        public double DefaultValue
        {
            get { return (double)this.GetValue(DefaultValueProperty); }
            set { this.SetValue(DefaultValueProperty, value); }
        }

        [Description("Interval"), Category("Common Properties")]
        public double Interval
        {
            get { return (double)this.GetValue(IntervalProperty); }
            set { this.SetValue(IntervalProperty, value); }
        }

        [Description("Suffix"), Category("Common Properties")]
        public string Suffix
        {
            get { return (string)this.GetValue(SuffixProperty); }
            set { this.SetValue(SuffixProperty, value); }
        }

        [Description("Maximum"), Category("Common Properties")]
        public double Maximum
        {
            get { return (double)this.GetValue(MaximumProperty); }
            set { this.SetValue(MaximumProperty, value); }
        }

        [Description("Minimum"), Category("Common Properties")]
        public double Minimum
        {
            get { return (double)this.GetValue(MinimumProperty); }
            set { this.SetValue(MinimumProperty, value); }
        }

        [Description("TrailingDigits"), Category("Common Properties")]
        public int TrailingDigits
        {
            get { return (int)this.GetValue(TrailingDigitsProperty); }
            set { this.SetValue(TrailingDigitsProperty, value); }
        }

        [Description("DelayedEventStartCount"), Category("Common Properties")]
        public int DelayedEventStartCount
        {
            get { return (int)this.GetValue(DelayedEventStartCountProperty); }
            set { this.SetValue(DelayedEventStartCountProperty, value); }
        }
        #endregion
    }

    [TemplatePart(Name = "Textbox", Type = typeof(TextBox))]
    public class DelayedTextBox : Control
    {
        public static readonly DependencyProperty DelayProperty;
        public static readonly DependencyProperty TextProperty;
        public static readonly DependencyProperty TextWrappingProperty;

        public static readonly RoutedEvent DelayedTextChangedEvent;
        private DispatcherTimer timer;
        private TextBox Textbox;

        private bool ignoretextchanggeflag = false;

        static DelayedTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DelayedTextBox), new FrameworkPropertyMetadata(typeof(DelayedTextBox)));

            DelayProperty = DependencyProperty.Register("Delay", typeof(int), typeof(DelayedTextBox), new PropertyMetadata(1000));
            TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(DelayedTextBox), new PropertyMetadata(""));
            TextWrappingProperty = DependencyProperty.Register("TextWrapping", typeof(TextWrapping), typeof(DelayedTextBox), new PropertyMetadata(TextWrapping.NoWrap));

            DelayedTextChangedEvent = EventManager.RegisterRoutedEvent("DelayedTextChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DelayedTextBox));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.GotKeyboardFocus += DelayedTextBox_GotKeyboardFocus;

            Textbox = this.GetTemplateChild("Textbox") as TextBox;
            Textbox.TextChanged += new TextChangedEventHandler(Textbox_TextChanged);
            Textbox.TextWrapping = TextWrapping;

            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, Delay);
            timer.Tick += new EventHandler(timer_Tick);
        }

        void DelayedTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            //throw new NotImplementedException();
            Textbox.Focus();
        }

        #region Events
        public event RoutedEventHandler DelayedTextChanged
        {
            add { AddHandler(DelayedTextChangedEvent, value); }
            remove { RemoveHandler(DelayedTextChangedEvent, value); }
        }

        void Textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!ignoretextchanggeflag)
            {
                timer.Stop();
                timer.Start();
            }
        }

        #endregion

        void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();

            RoutedEventArgs newEventArgs = new RoutedEventArgs(DelayedTextBox.DelayedTextChangedEvent);
            RaiseEvent(newEventArgs);
        }

        void ChangeTextWithoutEvent(string text)
        {
            ignoretextchanggeflag = true;
            Textbox.Text = text;
            ignoretextchanggeflag = false;
        }

        #region Properties
        [Description("Delay"), Category("Common Properties")]
        public int Delay
        {
            get { return (int)this.GetValue(DelayProperty); }
            set { this.SetValue(DelayProperty, value); }
        }

        [Description("Text"), Category("Common Properties")]
        public string Text
        {
            get { return (string)this.GetValue(TextProperty); }
            set { this.SetValue(TextProperty, value); }
        }

        [Description("TextWrapping"), Category("Common Properties")]
        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)this.GetValue(TextWrappingProperty); }
            set { this.SetValue(TextWrappingProperty, value); }
        }
        #endregion
    }
}

