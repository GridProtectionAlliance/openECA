using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VSynchroscope
{
    public partial class Form1 : Form
    {
        Timer t = new Timer();             

        //Tolerance Limits
        int phaseangle_tolerance=15;            //Phase Angle tolerance in degrees i.e +15/-15 degrees
        double frequency_tolerance = 0.067;     //Frequency_tolerance(maximum slip allowed) in Hz.
        double Vmag_tolerance=0.05;             //Voltage Magnitude tolerance in pu i.e. 0.95 pu to 1.05 p.u.
        double delay = 500;   //in ms
        
        //System data
        double RefFrequency = 60.0;
        double phasorFrequency = 60.2;
        int RefMag = 345;

        int WIDTH = 300, HEIGHT = 300;          //size and dimensions of bitmap
        int phasorMag = 150;                    //Initial V_mag size as per limitations
        double phasorvolt, Adv_angle;           //Adv_angle is a function of slip frequency and delay introduced 
        double u, u_initiated, u_adjusted, allowed_inititation_angle, allowed_termination_angle;  //in degrees
        int cx, cy;     //center of the circle
        int x, y;       //phasorMag coordinate
        int xnew, ynew,x_start,y_start,x_end,y_end;
        bool freq = true;       //freq boolean variable is a condition which is true if phasorFrequency>=RefFrequency
        bool breaker = false;   //breaker boolean variable is a condition which is true if only the breaker close command is accepted by the 
                                //algorithm and passed on to control units
        
        Bitmap bmp;
        Pen p;
        Graphics g;

        
        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)    //Lower Frequency Button
        {
            phasorFrequency = phasorFrequency - 0.033;            //steps of 0.033 Hz difference
            textBox4.Text = phasorFrequency.ToString("c").Remove(0, 1);     //display the updated PhasorFrequency and RefFrequency
            textBox5.Text = RefFrequency.ToString("c").Remove(0, 1);

            if (freq == true)           //freq boolean variable represents direction of rotation...if true then clockwise and vice versa
            {
                                
                if (phasorFrequency < RefFrequency)     //change direction of rotation if necessary
                
                {
                    freq = !freq;       //change direction of rotation
                    u = 360 - u;
                    if (RefFrequency-phasorFrequency<0.005)
                    {
                        t.Interval = 10000000;      //if slip is very minimal(<0.005 Hz) we assign a significantly large value to make the phasormag hand stop
                                                    //because following the formula gives t.Interval is nearly equal to infinity . 
                    }
                    else
                    {
                        t.Interval = Convert.ToInt32(1000 / (360 * (RefFrequency - phasorFrequency)));   //in milliseconds
                    }
                    
                }
                else
                {
                    if ((phasorFrequency - RefFrequency) < .005)
                    {
                        t.Interval = 10000000;
                    }
                    else //if(phasorFrequency>RefFrequency)
                    {
                       
                        t.Interval = Convert.ToInt32(1000 / (360 * (phasorFrequency - RefFrequency)));
                    }
                }
            }
            else
            {
                {
                    if (t.Interval <= 1) { t.Interval = t.Interval - 0; }     //t.Interval can't be negative  so stop below 1 millisecond
                   
                    else
                    {
                        if ((RefFrequency - phasorFrequency) < 0.005)
                        {
                            t.Interval = 10000000;
                        }
                        else
                        {
                            t.Interval = Convert.ToInt32(1000 / (360 * (RefFrequency - phasorFrequency)));
                        }
                    }
                }
            }
            
        }

        private void button4_Click(object sender, EventArgs e)      //Raise Freq Button
        {
            phasorFrequency = phasorFrequency + 0.033;              //steps of 0.033 Hz difference
            textBox4.Text = phasorFrequency.ToString("c").Remove(0, 1); //display the updated PhasorFrequency and RefFrequency
            textBox5.Text = RefFrequency.ToString("c").Remove(0, 1);
            if (freq == true)            //freq boolean variable represents direction of rotation...if true then clockwise and vice versa
            {
                if (t.Interval <= 1) { t.Interval = t.Interval - 0; }
               
                else
                {
                    if ((phasorFrequency - RefFrequency) < 0.005)       //if slip is very minimal(<0.005 Hz) we assign a significantly large value to make the phasormag hand stop
                                                                        //because following the formula gives t.Interval is nearly equal to infinity . 
                    { t.Interval = 10000000; }
                    else
                    {
                        t.Interval = Convert.ToInt32(1000 / (360 * (phasorFrequency - RefFrequency)));
                    }
                    
                }
            }
            
            else
            {
                
                if (phasorFrequency >RefFrequency)     //change direction of rotation  if necessary                               
                {
                    freq = !freq;
                    u = 360 - u;
                    if ((phasorFrequency - RefFrequency) < 0.005)
                    {
                        t.Interval = 10000000;
                    }
                    else
                    {
                       
                        t.Interval = Convert.ToInt32(1000 / (360 * (phasorFrequency - RefFrequency)));
                    }
                    
                }
                else 
                {
                    if ((RefFrequency - phasorFrequency)<0.005)
                    {
                        t.Interval = 10000000;
                    }
                    else //if (phasorFrequency < RefFrequency)
                    {
                        
                        t.Interval = Convert.ToInt32(1000 / (360 * (RefFrequency - phasorFrequency)));
                    }
                }
            }

        }

        private void button2_Click(object sender, EventArgs e)      //Lower Voltage Mag Button
        {
            phasorMag = phasorMag - 10;         //!0 due to size of graphics ....later in the algorithm  they have been scaled to Vmag accordingly.
           
        }

        private void button1_Click(object sender, EventArgs e)      //Raise Voltage Mag Button
        {
            phasorMag = phasorMag + 10;
            
        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            
        }

        private void label2_Click(object sender, EventArgs e)
        {
            
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)      //close Breaker
        {

            //Ideally phasorFrequency should be greater than Reffrequency so as to have power flowing forward.But provision for reverse Synch is also provided.We can remove that as needed.
            //Forward Case
            if (((phasorFrequency - RefFrequency) >= 0 && (phasorFrequency - RefFrequency) <= frequency_tolerance) && (phasorMag <= 180 && phasorMag >= 120))//check for frequency and voltage magnitude tolerance
                {
                if (allowed_inititation_angle>=180 && allowed_termination_angle>=180)
                {
                    if (u>=allowed_inititation_angle && u<=allowed_termination_angle)
                    {
                        breaker = true;             // condition to check breaker command is given and true
                        u_initiated = u;            //angle at which breaker command is given
                    }
                }
                else if(allowed_inititation_angle >= 180 && allowed_termination_angle <= 180)
                {
                    if ((u>=allowed_inititation_angle && u<=360)||(u>=0 && u<=allowed_termination_angle))
                    {
                        breaker = true;             // condition to check breaker command is given and true
                        u_initiated = u;            //angle at which breaker command is given
                    }
                }
                    
                }
            //Reverse Synch Case
            else if (((phasorFrequency - RefFrequency) < 0 && (RefFrequency - phasorFrequency) <= frequency_tolerance) && (phasorMag <= 180 && phasorMag >= 120))//check for frequency and voltage magnitude tolerance
            {
                if (allowed_inititation_angle <= 180 && allowed_termination_angle <= 180)
                {
                    if ((360-u) <= allowed_inititation_angle && (360-u) >= allowed_termination_angle)
                    {
                        breaker = true;             // condition to check breaker command is given and true
                        u_initiated = u;            //angle at which breaker command is given
                    }
                }
                else if (allowed_inititation_angle <= 180 && allowed_termination_angle >= 180)
                {
                    if (((360-u) <= allowed_inititation_angle && (360-u) >= 0) || ((360-u) <= 360 && (360-u) >= allowed_termination_angle))
                    {
                        breaker = true;             // condition to check breaker command is given and true
                        u_initiated = u;            //angle at which breaker command is given
                    }
                }

            }
        }
           
        //Open breaker provision would be removed if required as many other conditions have to be checked rather than simply disconnecting
        private void button8_Click(object sender, EventArgs e)      //open Breaker Button
        {
            t.Start();
            breaker = false;        //Reset breaker command boolean variable

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //create Bitmap
            bmp = new Bitmap(WIDTH + 70, HEIGHT + 70);

            //background color
            this.BackColor = Color.SeaShell;

            //center
            cx = WIDTH / 2 +35;
            cy = HEIGHT / 2 +35;

            //Also we have to include where to start as we have to see the zero crossing of both waveforms and then start from that particular angle
            //otherwise synchronization may look like done at 12 o'clock position but in reality may be much different.So u in reality would be
            //phase angle difference between Incoming phasor and reference phasor.
            
            //Here we start the application from zero degree initially
            //Initial degree of HAND
            u = 0;

            //Timer

            if (phasorFrequency >= RefFrequency)
            {
                if ((phasorFrequency - RefFrequency) < 0.005)
                {
                    t.Interval = 10000000;
                }
                else
                {
                    t.Interval = Convert.ToInt32(1000 / (360 * (phasorFrequency - RefFrequency)));      //in millisecond
                }        
            } 
            else
            {
                if ((RefFrequency - phasorFrequency) < 0.005)
                {
                    t.Interval = 10000000;
                }
                else
                {
                    t.Interval = Convert.ToInt32(1000 / (360 * (RefFrequency - phasorFrequency)));
                }
                    
            }

            t.Tick += new EventHandler(this.t_Tick);
            
            t.Start();
        }

        private void t_Tick(object sender, EventArgs e)
        {
            //pen
            p = new Pen(Color.SaddleBrown,5);

            //graphics
            g = Graphics.FromImage(bmp);

            
            //calculate x, y coordinate of HAND
            //double tu= (u - lim) % 360;
            if (freq==true)
            {
                if (u >= 0 && u <= 180)             //main hand
                {
                    //right half
                    //u in degree is converted into radian.

                    x = cx + (int)(phasorMag * Math.Sin(Math.PI * u / 180));
                    y = cy - (int)(phasorMag * Math.Cos(Math.PI * u / 180));
                }
                else   //left half
                {
                    x = cx - (int)(phasorMag * -Math.Sin(Math.PI * u / 180));
                    y = cy - (int)(phasorMag * Math.Cos(Math.PI * u / 180));
                }

               
            }
            else   //when incoming phasor is slower than ref phasor
            {
                if (u >= 0 && u <= 180)
                {
                    //right half
                    

                    x = cx + (int)(phasorMag * -Math.Sin(Math.PI * u / 180));
                    y = cy - (int)(phasorMag * Math.Cos(Math.PI * u / 180));
                }
                else
                {
                    x = cx - (int)(phasorMag * Math.Sin(Math.PI * u / 180));
                    y = cy - (int)(phasorMag * Math.Cos(Math.PI * u / 180));
                }

                
            }
            phasorvolt = RefMag + (phasorMag - 150) * Vmag_tolerance * RefMag / (3 * 10);   //I have made 3 clicks change 0.05 pu and that too due to limitation of graphics size.
            SolidBrush mybrush1 = new SolidBrush(Color.Khaki);
            Pen p2 = new Pen(Color.Black, 2);
            g.FillRectangle(new SolidBrush(Color.SeaShell), 0, 0, WIDTH + 100, HEIGHT + 100);       //Bitmap color fill
            g.FillEllipse(mybrush1, cx - WIDTH / 2, cy - HEIGHT / 2, WIDTH, HEIGHT);                //synchroscope color fill and outline
            g.DrawEllipse(p2, cx - WIDTH / 2, cy - HEIGHT / 2, WIDTH, HEIGHT);
            
            //Indication for Slow and Fast rotation 
            g.DrawString("SLOW", new Font("Arial", 20), Brushes.Red, new PointF(cx-120, HEIGHT / 3));
            g.DrawString("FAST", new Font("Arial", 20), Brushes.Green, new PointF(cx + 50, HEIGHT / 3));


            //Angle tolerance window
            SolidBrush mybrush3 = new SolidBrush(Color.PaleGreen);
            g.FillPie(mybrush3, cx-100, cy - WIDTH / 2, WIDTH - 100, HEIGHT, 255, 30);                               
            g.DrawPie(p2, cx-100, cy-WIDTH/2, WIDTH-100, HEIGHT, 255, 30);    //Angle tolerance window fill and outline
            

            //Draw reference phasor at 12 o'clock position
            g.DrawLine(p2, new Point(cx, cy - WIDTH / 2), new Point(cx, cy));

            
            //Draw advanced angle depending upon delay and freq diff(slip)            
            if ((phasorFrequency - RefFrequency)>=0)       //ideally Adv_angle should be calculated when incoming phasor freq >=Reffrequency 
               //right half of plane                       //as power flow should be from incoming island to Ref island but can be calculated both ways.
            {
                Adv_angle = (delay * 60 * 360 * (phasorFrequency - RefFrequency)) / (60 * 1000);
                xnew = cx + (int)(150 * -Math.Sin(Math.PI * Adv_angle / 180));
                ynew = cy - (int)(150 * Math.Cos(Math.PI * Adv_angle / 180));
            }
            else      //left half of plane
            {
                Adv_angle = (delay * 60 * 360 * (RefFrequency - phasorFrequency)) / (60 * 1000);
                xnew = cx - (int)(150 * -Math.Sin(Math.PI * Adv_angle / 180));
                ynew = cy - (int)(150 * Math.Cos(Math.PI * Adv_angle / 180));
            }
                       
            Pen p3 = new Pen(Color.Red, 4);
            p3.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
            g.DrawLine(p3, new Point(cx, cy), new Point(xnew, ynew));

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //To find out adjusted window including delays when Incoming Frequency> RefFrequency
            if ((phasorFrequency - RefFrequency) >= 0)
            {
                allowed_inititation_angle = 360 - phaseangle_tolerance - Adv_angle;
                if (Adv_angle >= phaseangle_tolerance)
                {
                    
                    allowed_termination_angle = 360 + phaseangle_tolerance - Adv_angle;
                }
                else
                {
                    
                    allowed_termination_angle = (phaseangle_tolerance - Adv_angle);
                }
                

            }
            //To find out adjusted window when Incoming Frequency< RefFrequency
            else
            {
                allowed_inititation_angle =  phaseangle_tolerance + Adv_angle;
                if (Adv_angle >= phaseangle_tolerance)
                {
                    
                    allowed_termination_angle = Adv_angle - phaseangle_tolerance;
                }
                else
                {
                    
                    allowed_termination_angle = 360-phaseangle_tolerance + Adv_angle;
                }
                
            }
            //Depict allowed breaker closing adjusted window after delay
            x_start = cx - (int)(150 * -Math.Sin(Math.PI * allowed_inititation_angle / 180));
            y_start = cy - (int)(150 * Math.Cos(Math.PI * allowed_inititation_angle / 180));
            g.DrawLine(new Pen(Color.SlateGray, 4), new Point(cx, cy), new Point(x_start, y_start));

            x_end = cx - (int)(150 * -Math.Sin(Math.PI * allowed_termination_angle / 180));
            y_end = cy - (int)(150 * Math.Cos(Math.PI * allowed_termination_angle / 180));
            g.DrawLine(new Pen(Color.SlateGray, 4), new Point(cx, cy), new Point(x_end, y_end));

            //Textbox Values representation
            textBox2.Text = phasorvolt.ToString("c").Remove(0, 1);
            textBox3.Text = RefMag.ToString("c").Remove(0, 1);
            textBox4.Text = phasorFrequency.ToString("c").Remove(0, 1);
            textBox5.Text = RefFrequency.ToString("c").Remove(0, 1);



            //Depict Adv_angle value in respective textbox
            textBox6.Text = Adv_angle.ToString("c").Remove(0, 1);
            
            //draw rotating phasorMag
            p.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
            g.DrawLine(p, new Point(cx, cy), new Point(x, y));

            //Center breaker indication initially green(open)
            SolidBrush mybrush2 = new SolidBrush(Color.Green);
            g.FillEllipse(mybrush2, cx - 30, cy - 30, WIDTH / 5, HEIGHT / 5);
            g.DrawEllipse(p2, cx - 30, cy - 30, WIDTH / 5, HEIGHT / 5);


            //indication of frequency requirements
            if (((phasorFrequency-RefFrequency)<=frequency_tolerance && phasorFrequency>=RefFrequency) || (RefFrequency-phasorFrequency<=frequency_tolerance && RefFrequency>phasorFrequency))
            {
                g.FillRectangle(new SolidBrush(Color.PaleGreen), cx-180,HEIGHT-20, 50, 25);
            }
            else { g.FillRectangle(new SolidBrush(Color.Salmon), cx - 180, HEIGHT - 20, 50, 25); }

            //indication of voltage requirements
            if(phasorMag>=120 && phasorMag<=180)
            {
                g.FillRectangle(new SolidBrush(Color.PaleGreen), cx +130, HEIGHT - 20, 50, 25);
            }
            else { g.FillRectangle(new SolidBrush(Color.Salmon), cx +130, HEIGHT - 20, 50, 25); }


            g.DrawString("Freq", new Font("Arial", 12), Brushes.Black, new PointF(cx - 180, HEIGHT - 20));
            g.DrawString("Vmag", new Font("Arial", 12), Brushes.Black, new PointF(cx + 130, HEIGHT - 20));

            //load bitmap in picturebox1
            pictureBox1.Image = bmp;

            //calculate final breaker closing angle (after final closing)
            if ((u_initiated + Adv_angle) >= 360)
            {
                u_adjusted = u_initiated + Adv_angle - 360;
            }
            else
            {
                u_adjusted = u_initiated + Adv_angle;
            }
            
            //update
            u++;                             //Later this "u" would be actually difference of(Incoming Phasor Angle-Ref Phasor angle)


            //Indication of breaker after u_adjusted is reached
            if (breaker == true) 
            {
                if ((int)(u -u_adjusted)==0)
                {
                    t.Stop();
                    g.FillEllipse(new SolidBrush(Color.Red), cx - 30, cy - 30, WIDTH / 5, HEIGHT / 5);
                    g.DrawEllipse(new Pen(Color.Black, 1f), cx - 30, cy - 30, WIDTH / 5, HEIGHT / 5);
                    pictureBox1.Image = bmp;
                    breaker = false;
                }
            }
            
            if (u == 360)
            {
                u = 0;

            }
            //dispose
            p.Dispose();
            g.Dispose();

        }

    }
}