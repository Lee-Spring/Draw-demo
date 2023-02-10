using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using static System.Windows.Forms.AxHost;
using System.Threading;
using static Draw.Form1;
using System.Security.Cryptography;
using System.Reflection;
using System.Reflection.Emit;

namespace Draw
{
    public partial class Form1 : Form
    {
        
        public Form1()
        {
            InitializeComponent();//初始化窗体
            this.StartPosition = FormStartPosition.CenterScreen;//窗体在屏幕中间显示
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);//双缓冲
            
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.UpdateStyles();  //减少闪烁？？
            this.Refresh();  //减少闪烁？？重绘控件？？



            //初始化pictureBox控件的大小
            pictureBox1.Width = this.Width - 4*pictureBox1.Left;//显示控件宽度=窗口宽度-4显示控件左外边距
            pictureBox1.Height = this.Height - pictureBox1.Top - 6*pictureBox1.Left;//显示控件高度=窗口高度-显示控件左上边距-6显示控件左外边距


            bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);//图层初始化，大小为pictureBox1大小
            

            //开始画布更新线程（自定义）
            toUpdate = new Thread(new ThreadStart(Run));
            toUpdate.IsBackground = true;//线程才会随着主线程的退出而退出
            toUpdate.Start();
        }

        //图形类别定义如下
        public enum ShapeType
        {
            Undefine = 0,//未定义
            Line = 1,//线
            Rectangle = 2,//矩形
            Circle = 3,//圆
            Ellipse = 4,//椭圆
            Ploygon = 5,//多边形
            Picture = 6,//图片
            Text = 7,//文本
        }

        Bitmap bmp = null;
        //Graphics g = null;  //定义Graphics对象实例
        //private Bitmap img = null;//用于显示在窗体上的图层
        public bool Isdrawing = false;//判断标记，是否为绘制时的鼠标移动
        public bool IsUpdate = true;//是否更新画板判断器
        public ShapeType drawselect = ShapeType.Undefine;//绘制图形选项
        Point startPoint;//绘制的起始点
        Point endPoint;//目标点
        Thread toUpdate;//图层刷新线程
        String strFileName;//文件对话框返回结果存储
        String strText;//模拟对话框返回结果存储
        List<Line> lines = new List<Line>();//用于存储line对象
        List<Rect> rectangles = new List<Rect>();//用于存储rect对象
        List<polygon> polygons = new List<polygon>();
        List<Ellipse> ellipses = new List<Ellipse>();
        List<Circle> circles = new List<Circle>();
        List<Picture> pictures = new List<Picture>();
        List<MyText> texts = new List<MyText>();
        List<ShapeType> shapeTypes = new List<ShapeType>();
        List<Point> tempPoints = new List<Point>();

        Stack<List<Shape>> undo = new Stack<List<Shape>>();
        Stack<List<Shape>> redo = new Stack<List<Shape>>();

        List<Shape> shapes = new List<Shape>();



        //图元基类声明
        public class Shape
        {
            public ShapeType type = ShapeType.Undefine;//图形类别初始化为“未定义”

            public List<Point> points = new List<Point>() { Point.Empty,Point.Empty}; //
            public LineStyle lineStyle = new LineStyle(); //
            public Color fillcolor = Color.White; //
            public float factor = 1.0f;
            //使用虚方法,以便在继承类中实现(重写),方便多态调用
            public virtual void Draw(Graphics g)
            {
            }
            public virtual void Save(StreamWriter sw)
            {
            }
            public virtual void Load(string str)
            {
            }   
            public virtual bool IsContain(Rectangle rect)
            {
                return false;
            }
        }

        //线型结构声明
        public struct LineStyle
        {
            public float Width;//线宽
            public Color Color;//颜色
            public DashStyle Style;//线型


            //创建构造函数，初始化各参数：颜色-黑色，线宽-1.0，线型-实线
            public LineStyle(Color color, float width = 1.0f, DashStyle style = DashStyle.Solid)
            {
                Width = width;
                Color = color;
                Style = style;
            }
            public LineStyle(float width, DashStyle style) : this(Color.Black, width, style)
            {
                //此处不用写代码，共享上面的构造函数内容
            }
        }

        //线-折线类声明
        public class Line : Shape
        {
            //public new List<Point> points = new List<Point>(2) { Point.Empty , Point.Empty };//“点”数组，用于存放折线上各端点
            //public LineStyle lineStyle = new LineStyle();//线型

            public Line()
            {
                type = ShapeType.Line;//图形类别：线
                //factor = Form1.enlarge_factor;
            }
            public override void Draw(Graphics g)
            {
                Pen pen = new Pen(lineStyle.Color, lineStyle.Width*factor);
                pen.DashStyle = lineStyle.Style;
                g.DrawLine(pen, points[0], points[1]);
            }
            public override void Save(StreamWriter sw)
            {
                sw.Write(points[0].X + ","+ points[0].Y + "," + points[1].X + "," + points[1].Y + "," + lineStyle.Width + "," + ColorTranslator.ToHtml(lineStyle.Color) + "," + lineStyle.Style);
                sw.Write(";");
            }
            public override void Load(string str)
            {
                string[] s = str.Split(',');
                if (s.Length == 7)
                {
                    try
                    {
                        points[0] = (new Point(Convert.ToInt32(s[0]), Convert.ToInt32(s[1])));
                        points[1] = (new Point(Convert.ToInt32(s[2]), Convert.ToInt32(s[3])));
                        lineStyle.Width = Convert.ToInt32(s[4]);
                        lineStyle.Color = ColorTranslator.FromHtml(s[5]);
                        lineStyle.Style = GetLineStypefromString(s[6]);
                    }
                    catch
                    {
                        Console.WriteLine("读取文件过程中存在异常，此文件可能已损坏或被人篡改");
                    }

                }
            }
            public override bool IsContain(Rectangle rect)
            {
                foreach(Point point in points)
                {
                    if (rect.Contains(point))
                    {
                        return true;
                    }
                }
                return false;
            }

        }

        //矩形类声明
        public class Rect : Shape
        {
            //public List<Point> points = new List<Point>(2) { Point.Empty, Point.Empty };
            //public LineStyle lineStyle = new LineStyle();//矩形边界线线型
            //public Color fillcolor = Color.White;//填充颜色
            public Rect()
            {
                type = ShapeType.Rectangle;//图形类别：矩形
            }
            public override void Draw(Graphics g)
            {
                Point point0 = points[0];
                Point point1 = points[1]; 
                int min_x = Math.Min(point0.X, point1.X);
                int min_y = Math.Min(point0.Y, point1.Y); 
                int width = Math.Abs(point0.X - point1.X);
                int height = Math.Abs(point0.Y - point1.Y);

                Rectangle rect = new Rectangle(min_x, min_y, width, height);



                //绘制矩形有一个特点，就是只能向点的右下方绘制
                //这个时候，width和heigth的正负可以帮助正确绘制矩形
                Pen p = new Pen(lineStyle.Color, lineStyle.Width*factor);
                p.DashStyle = lineStyle.Style;
                Brush brush = new SolidBrush(fillcolor);
                g.DrawRectangle(p, rect);
                //g.FillRectangle(brush, rect);

            }
            public override void Save(StreamWriter sw)
            {
                sw.Write(points[0].X + "," + points[0].Y + "," + points[1].X + "," + points[1].Y + "," + lineStyle.Width + "," + ColorTranslator.ToHtml(lineStyle.Color) + "," + lineStyle.Style + "," + ColorTranslator.ToHtml(fillcolor));
                sw.Write(";");
            }
            public override void Load(string str)
            {
                string[] s = str.Split(',');
                points[0]=(new Point(Convert.ToInt32(s[0]), Convert.ToInt32(s[1])));
                points[1]=(new Point(Convert.ToInt32(s[2]), Convert.ToInt32(s[3])));
                lineStyle.Width = Convert.ToInt32(s[4]);
                lineStyle.Color = ColorTranslator.FromHtml(s[5]);
                lineStyle.Style = GetLineStypefromString(s[6]);
                fillcolor = ColorTranslator.FromHtml(s[7]);
            }
            public override bool IsContain(Rectangle rect)
            {
                foreach (Point point in points)
                {
                    if (rect.Contains(point))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        //多边形类声明
        public class polygon : Shape
        {
            //public List<Point> Points = new List<Point>();//存放边界“点”数组
            //public LineStyle lineStyle = new LineStyle();//边界线线型
            //public Color fillcolor = Color.White;//填充颜色
            public polygon()
            {
                type =  ShapeType.Ploygon;
            }
            public override void Draw(Graphics g)
            {
                Pen p = new Pen(lineStyle.Color, lineStyle.Width*factor);
                p.DashStyle = lineStyle.Style;
                SolidBrush br = new SolidBrush(fillcolor);//填充
                if (points.Count > 2)
                {
                    g.FillPolygon(br, points.ToArray());
                    g.DrawPolygon(p, points.ToArray());
                    
                }

            }
            public override void Save(StreamWriter sw)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    sw.Write(points[i].X + ",");
                    sw.Write(points[i].Y + ",");
                }
                sw.Write(lineStyle.Width + "," + ColorTranslator.ToHtml(lineStyle.Color) + "," + lineStyle.Style + "," + ColorTranslator.ToHtml(fillcolor));
                sw.Write(";");
            }
            public override void Load(string str)
            {
                string[] s = str.Split(',');
                points.Clear();
                for (int i = 0; i < s.Length-4; i+=2)
                {
                    points.Add(new Point(Convert.ToInt32(s[i]), Convert.ToInt32(s[i + 1])));
                }
                lineStyle.Width = Convert.ToInt32(s[s.Length-4]);
                lineStyle.Color = ColorTranslator.FromHtml(s[s.Length - 3]);
                lineStyle.Style = GetLineStypefromString(s[s.Length - 2]);
                fillcolor = ColorTranslator.FromHtml(s[s.Length - 1]);
            }
            public override bool IsContain(Rectangle rect)
            {
                foreach (Point point in points)
                {
                    if (rect.Contains(point))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        //椭圆类声明，父类为Rect
        public class Ellipse : Rect
        {
            //矩形区域、边界线、填充颜色已在类Rect中声明
            public Ellipse()
            {
                type= ShapeType.Ellipse;
            }
            public override void Draw(Graphics g)
            {
                Point point0 = points[0];
                Point point1 = points[1];
                int min_x = Math.Min(point0.X, point1.X);
                int min_y = Math.Min(point0.Y, point1.Y);
                int width = Math.Abs(point0.X - point1.X);
                int height = Math.Abs(point0.Y - point1.Y);

                Rectangle rect = new Rectangle(min_x, min_y, width, height);

                Pen p = new Pen(lineStyle.Color, lineStyle.Width*factor);
                p.DashStyle = lineStyle.Style;
                Brush brush = new SolidBrush(fillcolor);
                g.DrawEllipse(p, rect);
                g.FillEllipse(brush, rect);
            }
            public override void Save(StreamWriter sw)
            {
                base.Save(sw);
            }
            public override void Load(string str)
            {
                string[] s = str.Split(',');
                points[0] = (new Point(Convert.ToInt32(s[0]), Convert.ToInt32(s[1])));
                points[1] = (new Point(Convert.ToInt32(s[2]), Convert.ToInt32(s[3])));
                lineStyle.Width = Convert.ToInt32(s[4]);
                lineStyle.Color = ColorTranslator.FromHtml(s[5]);
                lineStyle.Style = GetLineStypefromString(s[6]);
                fillcolor = ColorTranslator.FromHtml(s[7]);
            }
            public override bool IsContain(Rectangle rect)
            {
                foreach (Point point in points)
                {
                    if (rect.Contains(point))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        //圆类声明
        public class Circle : Ellipse
        {
            //矩形区域、边界线、填充颜色已在类Ellipse中声明
            public int Radiu//圆的半径：取矩形区域的宽
            {
                get { return Math.Min(Math.Abs(points[0].X - points[1].X),Math.Abs( points[0].Y - points[1].Y)); }
            }
            public Circle()
            {
                type = ShapeType.Circle;
            }
            public override void Draw(Graphics g)
            {
                Point point0 = points[0];
                Point point1 = points[1];
                int min_x = point0.X;
                int min_y = point0.Y;
                if(point0.X > point1.X)
                {
                    min_x -= Radiu;
                }
                if(point0.Y > point1.Y)
                {
                    min_y -= Radiu;
                }
                
                Rectangle rect = new Rectangle(min_x, min_y, Radiu, Radiu);
                //Rectangle rect = new Rectangle(points[0].X, points[0].Y,Radiu,Radiu);
                Pen p = new Pen(lineStyle.Color, lineStyle.Width*factor);
                p.DashStyle = lineStyle.Style;
                Brush brush = new SolidBrush(fillcolor);
                g.DrawEllipse(p, rect);
                g.FillEllipse(brush, rect);
            }
            public override void Save(StreamWriter sw)
            {
                base.Save(sw);
            }
            public override void Load(string str)
            {
                string[] s = str.Split(',');
                points[0] = (new Point(Convert.ToInt32(s[0]), Convert.ToInt32(s[1])));
                points[1] = (new Point(Convert.ToInt32(s[2]), Convert.ToInt32(s[3])));
                lineStyle.Width = Convert.ToInt32(s[4]);
                lineStyle.Color = ColorTranslator.FromHtml(s[5]);
                lineStyle.Style = GetLineStypefromString(s[6]);
                fillcolor = ColorTranslator.FromHtml(s[7]);
            }
            public override bool IsContain(Rectangle rect)
            {
                foreach (Point point in points)
                {
                    if (rect.Contains(point))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        //图片类声明
        public class Picture : Rect
        {
            //图片绘制在矩形区域
            public Bitmap img = null;
            public String filename = null;
            public Picture()
            {
                type = ShapeType.Picture;
            }
            public override void Draw(Graphics g)
            {
                Point point0 = points[0];
                Point point1 = points[1];
                int min_x = Math.Min(point0.X, point1.X);
                int min_y = Math.Min(point0.Y, point1.Y);
                int width = Math.Abs(point0.X - point1.X);
                int height = Math.Abs(point0.Y - point1.Y);

                Rectangle rect = new Rectangle(min_x, min_y, width, height);

                img = new Bitmap(filename);
                g.DrawImage(img,rect);
            }
            public override void Save(StreamWriter sw)
            {
                sw.Write(points[0].X + "," + points[0].Y +","+ points[1].X +","+ points[1].Y + "," + filename);
                sw.Write(";");
            }
            public override void Load(string str)
            {
                string[] s = str.Split(',');
                points[0] = (new Point(Convert.ToInt32(s[0]), Convert.ToInt32(s[1])));
                points[1] = (new Point(Convert.ToInt32(s[2]), Convert.ToInt32(s[3])));
                filename = s[4];
            }
            public override bool IsContain(Rectangle rect)
            {
                foreach (Point point in points)
                {
                    if (rect.Contains(point))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        //文本类声明
        public class MyText : Rect
        {
            //文字显示在矩形区域
            public string Text = "";
            public string fontName = "宋体";
            public float fontSize = 10.0f;
            public Color textColor = Color.Black;
            public MyText()
            {
                type =   ShapeType.Text;
            }
            public override void Draw(Graphics g)
            {
                Font font = new Font(fontName,fontSize*factor);
                g.DrawString(Text, font, new SolidBrush(textColor), points[0]);
            }
            public override void Save(StreamWriter sw)
            {
                sw.Write(points[0].X +","+ points[0].Y + "," + Text + "," + fontName + "," + fontSize + "," + ColorTranslator.ToHtml(textColor));
                sw.Write(";");
            }
            public override void Load(string str)
            {
                string[] s = str.Split(',');
                points[0] = (new Point(Convert.ToInt32(s[0]), Convert.ToInt32(s[1])));
                Text = s[2];
                fontName = s[3];
                fontSize = Convert.ToInt32(s[4]);
                textColor = ColorTranslator.FromHtml(s[5]);
            }
            public override bool IsContain(Rectangle rect)
            {
                foreach (Point point in points)
                {
                    if (rect.Contains(point))
                    {
                        return true;
                    }
                }
                return false;
            }
        }



        private void 帮助ToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void 绘制ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void 线条设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        
        private void solidToolStripMenuItem_Click(object sender, EventArgs e)//选择实线并展示
        {
            线型toolStripDropDownButton.Text = "0";
            线型toolStripDropDownButton.ToolTipText = "Solid";
            线型toolStripDropDownButton.Image = System.Drawing.Image.FromFile("icon\\实线.png");
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)//选择点线并展示
        {
            线型toolStripDropDownButton.Text = "2";
            线型toolStripDropDownButton.ToolTipText = "Dot";
            线型toolStripDropDownButton.Image = System.Drawing.Image.FromFile("icon\\点线.png");
        }

        private void dashToolStripMenuItem_Click(object sender, EventArgs e)//选择Dash并展示
        {
            线型toolStripDropDownButton.Text = "1";
            线型toolStripDropDownButton.ToolTipText = "Dash";
            线型toolStripDropDownButton.Image = System.Drawing.Image.FromFile("icon\\虚线.png");
        }

        private void 颜色toolStripButton_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();//新建一个颜色对话框
            cd.AllowFullOpen = true;//“规定自定义颜色”按钮
            cd.FullOpen = true;//显示自定义颜色部分
            cd.ShowHelp = true;//帮助按钮
            var result = cd.ShowDialog();//打开颜色对话框，并接收对话框操作结果
            if (result == DialogResult.OK)//如果用户点击OK
            {
                var color = cd.Color;//获取用户选择的颜色
                颜色toolStripButton.BackColor = color;
            }
        }

        private void 填充颜色toolStripButton_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();//新建一个颜色对话框
            cd.AllowFullOpen = true;//“规定自定义颜色”按钮
            cd.FullOpen = true;//显示自定义颜色部分
            cd.ShowHelp = true;//帮助按钮
            var result = cd.ShowDialog();//打开颜色对话框，并接收对话框操作结果
            if (result == DialogResult.OK)//如果用户点击OK
            {
                var color = cd.Color;//获取用户选择的颜色
                填充颜色toolStripButton.BackColor = color;
            }
        }

        public void 直线toolStripButton_Click(object sender, EventArgs e)
        {
            Isdrawing = false;
            drawselect = ShapeType.Line;//图形选择为直线
            this.Cursor = Cursors.Cross;//鼠标光标变为“十”字
        }

        private void 矩形toolStripButton_Click(object sender, EventArgs e)
        {
            Isdrawing = false;
            drawselect = ShapeType.Rectangle;//图形选择为矩形
            this.Cursor = Cursors.Cross;//鼠标光标变为“十”字
        }

        public bool IsPloygon = false;
        private void 多边形toolStripButton_Click(object sender, EventArgs e)
        {
            Isdrawing = false;
            drawselect = ShapeType.Ploygon;//图形选择为多边形
            this.Cursor = Cursors.Cross;//鼠标光标变为“十”字
            IsPloygon = true;
        }

        private void 圆toolStripButton_Click(object sender, EventArgs e)
        {
            Isdrawing = false;
            drawselect = ShapeType.Circle;//图形选择为圆
            this.Cursor = Cursors.Cross;//鼠标光标变为“十”字
        }

        private void 椭圆toolStripButton_Click(object sender, EventArgs e)
        {
            Isdrawing = false;
            drawselect = ShapeType.Ellipse;//图形选择为椭圆
            this.Cursor = Cursors.Cross;//鼠标光标变为“十”字
        }

        private void 图像toolStripButton_Click(object sender, EventArgs e)
        {
            Isdrawing = false;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "打开图片";
            ofd.Filter = "位图文件(*.png;*.jpg)|*.png;*.jpg|矢量文件(*.shp;*.svg)|*.shp;*.svg";
            ofd.ValidateNames = true; // 验证用户输入是否是一个有效的Windows文件名
            ofd.CheckFileExists = true; //验证文件的有效性
            ofd.CheckPathExists = true;//验证路径的有效性
            if (ofd.ShowDialog() == DialogResult.OK) //用户点击确认按钮，发送确认消息
            {
                strFileName = ofd.FileName;//获取在文件对话框中选定的路径或者字符串
                drawselect = ShapeType.Picture;//图形选择为图像
                this.Cursor = Cursors.Cross;//鼠标光标变为“十”字

                ofd.Dispose();
                //strFileName = null;
            }

        }

        private void 文本toolStripButton_Click(object sender, EventArgs e)
        {
            Isdrawing = false;
            FontDialog fontDialog = new FontDialog();//新建一个字体对话框
            fontDialog.AllowScriptChange = true;
            fontDialog.ShowColor = true;
            var result = fontDialog.ShowDialog();//打开颜色对话框，并接收对话框操作结果
            if (result == DialogResult.OK)//如果用户点击OK
            {
                var font = fontDialog.Font;//获取用户选择的颜色
                var fontColor = fontDialog.Color;
                线宽toolStripComboBox.Font = Font;
                线宽toolStripComboBox.ForeColor = fontColor;
                drawselect = ShapeType.Text;//图形选择为文本
                this.Cursor = Cursors.Cross;//鼠标光标变为“十”字

                fontDialog.Dispose();
            }
        }














        bool IsMoving = false;
        Point OldPoint = Point.Empty;
        Point LastPoint = Point.Empty;
        int dx = 0;
        int dy = 0;






        //鼠标按下
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {


            if (this.Cursor == Cursors.SizeAll)
            {
                IsMoving = true;
                OldPoint = e.Location;

            }

            //鼠标光标为“十”字作为绘制状态的标志
            //如果鼠标光标为“十”字，此时按下鼠标左键就可以开始进行绘制
            if (this.Cursor == Cursors.Cross)
            {
                startPoint = new Point(e.X, e.Y);//起始点坐标为鼠标按下位置的点坐标
                endPoint = new Point(e.X, e.Y);//结尾点（目标点）坐标暂定为鼠标按下位置的点坐标，后面会随着鼠标移动而更新
                Isdrawing = true;//按下鼠标后，移动鼠标可以进行绘制图形

                switch (drawselect)
                {
                    case ShapeType.Line://直线
                        {
                            /*
                            if (drawingShape.type == ShapeType.Line)
                            {
                                shapes.Add(drawingShape);
                            }

                            drawingShape = new Line();
                            shapeTypes.Add(ShapeType.Line);
                            drawingShape.points[0] = startPoint;
                            drawingShape.points[1] = endPoint;
                            if (!float.TryParse(线宽toolStripComboBox.Text, out drawingShape.lineStyle.Width))//线宽文本框中的字符串转化为float类型赋值给图形线宽
                            {
                                MessageBox.Show("请输入正确的线宽！");//若输入的值无法转换为float类型则报错，且文本框恢复默认值“1.0”
                                线宽toolStripComboBox.Text = "1.0";
                            }
                            drawingShape.lineStyle.Style = (DashStyle)int.Parse(线型toolStripDropDownButton.Text);
                            */


                            undo.Push(shapes);

                            Line line1 = new Line();//实例化一个 Line 对象
                            shapeTypes.Add(ShapeType.Line);

                            line1.points[0] = (startPoint);//将按下鼠标的坐标作为起始点，存入到line list中作为直线起始点
                            line1.points[1] = (endPoint);
                            line1.lineStyle.Color = 颜色toolStripButton.BackColor;//线的颜色替换为选择的颜色（即展示在工具栏中的颜色）
                            if (!float.TryParse(线宽toolStripComboBox.Text, out line1.lineStyle.Width))//线宽文本框中的字符串转化为float类型赋值给图形线宽
                            {
                                MessageBox.Show("请输入正确的线宽！");//若输入的值无法转换为float类型则报错，且文本框恢复默认值“1.0”
                                线宽toolStripComboBox.Text = "1.0";
                            }
                            line1.lineStyle.Style = (DashStyle)int.Parse(线型toolStripDropDownButton.Text);//用户选择的线型存储在线型toolStripDropDownButton.Text中（string：0，1，2），转换为int后再转换为(DashStyle)
                            lines.Add(line1);//将 Line 对象添加到直线图形列表中存储


                            shapes.Add(line1);



                            break;
                        }
                    case ShapeType.Rectangle://矩形
                        {
                            //参数设置基本和直线相似
                            Rect rect1 = new Rect();//
                            undo.Push(new List<Shape>(shapes));

                            rect1.points[0] = (startPoint);
                            rect1.points[1] = (endPoint);
                            rect1.lineStyle.Color = 颜色toolStripButton.BackColor;
                            if (!float.TryParse(线宽toolStripComboBox.Text, out rect1.lineStyle.Width))//线宽文本框中的字符串转化为float类型赋值给图形线宽
                            {
                                MessageBox.Show("请输入正确的线宽！");//若输入的值无法转换为float类型则报错，且文本框恢复默认值“1.0”
                                线宽toolStripComboBox.Text = "1.0";
                            }
                            rect1.lineStyle.Style = (DashStyle)int.Parse(线型toolStripDropDownButton.Text);
                            rect1.fillcolor = 填充颜色toolStripButton.BackColor;
                            rectangles.Add(rect1);
                            shapes.Add(rect1); //

                            break;
                        }
                    case ShapeType.Ploygon://多边形
                        {
                            if (IsPloygon)
                            {
                                IsPloygon = false;
                                polygon polygon1 = new polygon();
                                shapeTypes.Add(ShapeType.Ploygon);

                                polygon1.lineStyle.Color = 颜色toolStripButton.BackColor;
                                if (!float.TryParse(线宽toolStripComboBox.Text, out polygon1.lineStyle.Width))//线宽文本框中的字符串转化为float类型赋值给图形线宽
                                {
                                    MessageBox.Show("请输入正确的线宽！");//若输入的值无法转换为float类型则报错，且文本框恢复默认值“1.0”
                                    线宽toolStripComboBox.Text = "1.0";
                                }
                                polygon1.lineStyle.Style = (DashStyle)int.Parse(线型toolStripDropDownButton.Text);
                                polygon1.fillcolor = 填充颜色toolStripButton.BackColor;
                                polygon1.points[0] = (startPoint);
                                polygon1.points[1] = (endPoint);
                                tempPoints.Add(startPoint);
                                tempPoints.Add(endPoint);
                                polygons.Add(polygon1);
                                shapes.Add(polygon1);
                            }
                            else
                            {
                                polygons[polygons.Count-1].points.Add(startPoint);
                                tempPoints[tempPoints.Count-1] = (startPoint);
                                tempPoints.Add(endPoint);

                                undo.Push(new List<Shape>(shapes));
                            }
                            break;
                        }

                    case ShapeType.Ellipse:
                        {
                            Ellipse ellipse1 = new Ellipse();
                            shapeTypes.Add(ShapeType.Ellipse);
                            ellipse1.points[0] = (startPoint);
                            ellipse1.points[1] = (endPoint);
                            ellipse1.lineStyle.Color = 颜色toolStripButton.BackColor;
                            if (!float.TryParse(线宽toolStripComboBox.Text, out ellipse1.lineStyle.Width))//线宽文本框中的字符串转化为float类型赋值给图形线宽
                            {
                                MessageBox.Show("请输入正确的线宽！");//若输入的值无法转换为float类型则报错，且文本框恢复默认值“1.0”
                                线宽toolStripComboBox.Text = "1.0";
                            }
                            ellipse1.lineStyle.Style = (DashStyle)int.Parse(线型toolStripDropDownButton.Text);
                            ellipse1.fillcolor = 填充颜色toolStripButton.BackColor;
                            ellipses.Add(ellipse1);
                            shapes.Add(ellipse1);
                            break;
                        }
                    case ShapeType.Circle:
                        {
                            Circle circle1 = new Circle();
                            shapeTypes.Add(ShapeType.Circle);
                            circle1.points[0] = (startPoint);
                            circle1.points[1] = (endPoint);
                            circle1.lineStyle.Color = 颜色toolStripButton.BackColor;
                            if (!float.TryParse(线宽toolStripComboBox.Text, out circle1.lineStyle.Width))//线宽文本框中的字符串转化为float类型赋值给图形线宽
                            {
                                MessageBox.Show("请输入正确的线宽！");//若输入的值无法转换为float类型则报错，且文本框恢复默认值“1.0”
                                线宽toolStripComboBox.Text = "1.0";
                            }
                            circle1.lineStyle.Style = (DashStyle)int.Parse(线型toolStripDropDownButton.Text);
                            circle1.fillcolor = 填充颜色toolStripButton.BackColor;
                            circles.Add(circle1);
                            shapes.Add(circle1);
                            break;
                        }
                    case ShapeType.Picture:
                        {
                            Picture picture1 = new Picture();
                            shapeTypes.Add(ShapeType.Picture);
                            picture1.filename = strFileName;
                            picture1.points[0] = (startPoint);
                            picture1.points[1] = (endPoint);
                            picture1.lineStyle.Color = 颜色toolStripButton.BackColor;
                            if (!float.TryParse(线宽toolStripComboBox.Text, out picture1.lineStyle.Width))//线宽文本框中的字符串转化为float类型赋值给图形线宽
                            {
                                MessageBox.Show("请输入正确的线宽！");//若输入的值无法转换为float类型则报错，且文本框恢复默认值“1.0”
                                线宽toolStripComboBox.Text = "1.0";
                            }
                            picture1.lineStyle.Style = (DashStyle)int.Parse(线型toolStripDropDownButton.Text);
                            pictures.Add(picture1);
                            shapes.Add(picture1);
                            break;
                        }
                    case ShapeType.Text:
                        {
                            Form2 form2 = new Form2();
                            DialogResult result = form2.ShowDialog();
                            if (result == DialogResult.OK)
                            {
                                strText = form2.text2;

                                MyText text1 = new MyText();
                                shapeTypes.Add(ShapeType.Text);

                                text1.Text = strText;
                                text1.points[0] = (endPoint);
                                text1.fontName = 线宽toolStripComboBox.Font.SystemFontName;
                                text1.fontSize = 线宽toolStripComboBox.Font.SizeInPoints;
                                text1.textColor = 线宽toolStripComboBox.ForeColor;
                                texts.Add(text1);
                                shapes.Add(text1);
                            }
                            undo.Push(new List<Shape>(shapes));
                            break;
                        }
                }


            if (this.Cursor == Cursors.Cross && e.Button == MouseButtons.Right)
                {
                    DrawToSelect();
                }
            if (this.Cursor == Cursors.Default && e.Button == MouseButtons.Right)
                {

                }

            }
        }

        //鼠标移动
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {

            //button1.Text = Isdrawing.ToString();//用于检测的按钮
            //
            if (this.Cursor == Cursors.SizeAll && IsMoving == true)
            {
                LastPoint = e.Location;

                dx = (LastPoint.X - OldPoint.X)/40;
                dy = (LastPoint.Y - OldPoint.Y)/40;
                for (int i = 0; i < shapes.Count; i++)
                {
                    for (int j = 0; j < shapes[i].points.Count; j++)
                    {
                        Point pt = shapes[i].points[j];
                        pt.Offset(dx,dy);
                        shapes[i].points[j] = pt;
                    }
                }


                //button1.Text = dx.ToString()+" "+LastPoint.X.ToString()+" "+ OldPoint.X.ToString();
            }

            //根据鼠标光标的样式，判断鼠标是处于绘制、选择、选中、移动等模式
            if (this.Cursor == Cursors.Cross && Isdrawing)//若鼠标光标样式为“十字”，判定为绘制模式，若处于绘制状态Isdrawing，移动鼠标将绘制相应的图形
            {
                Point now_point = new Point(e.X, e.Y);//实例一个Point对象，为此时鼠标的坐标

                switch (drawselect)//相应图形的鼠标移动事件
                {
                    
                    case ShapeType.Line://直线
                        {
                           
                            //绘制对象为line的话，再鼠标按下事件中已经存储了新的line对象
                            //所以，在这里直接再lines列表中
                            try
                            {
                                
                                lines[lines.Count - 1].points[1] = now_point;
                                //Graphics g = pictureBox1.CreateGraphics();
                                //lines[lines.Count - 1].Draw(g); 
                                //lines[lines.Count - 1].Draw(g);
                                //g.DrawLine(pen, startPoint, new Point(e.X, e.Y));
                            }
                            catch (System.ArgumentOutOfRangeException)
                            {

                            }

                            break;
                        }
                    case ShapeType.Rectangle:
                        {
                            //Rect rect1 = new Rect();
                            //rect1 = rectangles[rectangles.Count - 1];
                            //rect1.points[1] = now_point;

                            rectangles[rectangles.Count - 1].points[1] = now_point;

                            break;
                        }
                    case ShapeType.Ploygon:
                        {
                            tempPoints[tempPoints.Count-1] = now_point;
                            break;
                        }
                    case ShapeType.Ellipse:
                        {
                            Ellipse ellipse1 = new Ellipse();

                            ellipse1 = ellipses[ellipses.Count - 1];
                            ellipse1.points[1] = now_point;
                            break;
                        }
                    case ShapeType.Circle:
                        {
                            Circle circle1 = new Circle();

                            circle1 = circles[circles.Count - 1];
                            circle1.points[1] = e.Location;
                            break;
                        }
                    case ShapeType.Picture:
                        {
                            Picture picture1 = new Picture();
                            picture1 = pictures[pictures.Count - 1];
                            picture1.points[1] = now_point;
                            break;
                        }
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            IsMoving = false;
            if (drawselect == ShapeType.Line || drawselect == ShapeType.Ploygon)
            {
                Isdrawing = true;//按下鼠标后，移动鼠标可以进行绘制图形
            }
            else
            {
                Isdrawing = false;//除直线外，其它图形在鼠标抬起的时候不能绘制

                undo.Push(new List<Shape>(shapes));

            }

        }


        //鼠标双击事件，此事件主要用来取消直线的绘制
        private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            undo.Push(new List<Shape>(shapes));
            DrawToSelect();
        }

        private void Run()//刷新图层函数
        {
            while (IsUpdate)
            {
                try//当用户关闭窗体时，pictureBox1对象会销毁，此时线程仍在继续就会出现异常，故在这里加了异常捕获
                {
                    //pictureBox1.Invalidate();
                    //获取内存画布的Graphics引用：
                    Graphics g = Graphics.FromImage(bmp);
                    g.Clear(pictureBox1.BackColor);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    if (shapes.Count > 0)
                    {
                        if (shapes[shapes.Count - 1].type == ShapeType.Ploygon && this.Cursor == Cursors.Cross && Isdrawing && drawselect==ShapeType.Ploygon)
                        {
                            for (int i = 0; i < shapes.Count - 1; i++)
                            {
                                shapes[i].Draw(g);
                            }
                            Pen pen = new Pen(shapes[shapes.Count - 1].lineStyle.Color, shapes[shapes.Count - 1].lineStyle.Width);

                            g.DrawLines(pen, tempPoints.ToArray());
                        }
                        else
                        {
                            foreach( var shape in shapes.ToArray())
                            {
                                shape.Draw(g);
                            }
                        }
                    }
                    try
                    {
                        Graphics g2 = pictureBox1.CreateGraphics();
                        g2.DrawImage(bmp, 0, 0);
                        g2.Dispose();
                    }
                    catch
                    {
                        Console.WriteLine("最好点击退出按钮退出程序，而不是使用窗体上的×");
                    }
                }
                catch (ObjectDisposedException e)
                {
                    Console.WriteLine("最好点击退出按钮退出程序，而不是使用窗体上的×");
                    break;
                }

                

                Thread.Sleep(50);                 //每2秒钟刷新一次  
            }
        }

        private void 选择toolStripButton_Click(object sender, EventArgs e)
        {
            DrawToSelect();
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void Form1_Resize(object sender, EventArgs e)//更新pictureBox的大小，即画布大小随窗体缩放而缩放
        {
            pictureBox1.Width = this.Width - 4 * pictureBox1.Left;//显示控件宽度=窗口宽度-显示控件左外边距
            pictureBox1.Height = this.Height - pictureBox1.Top - 6 * pictureBox1.Left;//显示控件高度=窗口高度-显示控件左上边距
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            //在OnPaint方法中实现下面代码

            /*
            Graphics g = pictureBox1.CreateGraphics();
            if (g == null) return;
            if (g != null)
                {
                    g.DrawImage(bmp, 0, 0);
                }
            */
        }

        private void 保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IsUpdate = false;
            toUpdate.DisableComObjectEagerCleanup();//清理线程
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "图像文件|*.jpg|图像文件|*.png|矢量文件|*.lcx";
            saveFile.Title = "保存文件";
            saveFile.DefaultExt = "图像文件|*.jpg";//设置文件默认扩展名 
            saveFile.InitialDirectory = @"C:\Users\Administrator\Desktop";//设置保存的初始目录
            if (saveFile.ShowDialog() == DialogResult.OK)
            {
                //用户点击"保存"后执行的代码
                string suffix = saveFile.FileName.Substring(saveFile.FileName.Length - 3);
                if ( suffix == "jpg" || suffix == "png")
                {
                    //Bitmap bitmap = new Bitmap(pictureBox1.Image);
                    Bitmap bitmap = (Bitmap)bmp.Clone();
                    bitmap.Save(saveFile.FileName);
                }
                else//.lcx是我自定义的文件格式
                {
                    //首先写入文件标识区
                    FileStream fs = new FileStream(saveFile.FileName, FileMode.Create, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(fs);
                    sw.WriteLine("成都理工大学李春肖");
                    shapeWrite(sw);
                    sw.Close();
                    fs.Close();
                }
                MessageBox.Show("保存成功！");
            }
            IsUpdate = true;
            toUpdate = new Thread(new ThreadStart(Run));
            toUpdate.IsBackground = true;//线程才会随着主线程的退出而退出
            toUpdate.Start();
        }

        void shapeWrite(StreamWriter sw)
        {
            string s = null;
            for (int i = 0; i < shapeTypes.Count; i++)
            {
                s += shapeTypes[i];
                s += ",";
            }
            sw.WriteLine(s);

            foreach (var line in lines)
            {
                line.Save(sw);
            }

            sw.WriteLine();
            foreach (var rect in rectangles)
            {
                rect.Save(sw);
            }

            sw.WriteLine();
            foreach (var polygon in polygons)
            {
                polygon.Save(sw);
            }

            sw.WriteLine();
            foreach (var ellipse in ellipses)
            {
                ellipse.Save(sw);
            }

            sw.WriteLine();
            foreach (var circle in circles)
            {
                circle.Save(sw);
            }

            sw.WriteLine();
            foreach (var picture in pictures)
            {
                picture.Save(sw);
            }

            sw.WriteLine();
            foreach (var text in texts)
            {
                text.Save(sw);
            }
            sw.WriteLine();

        }


        private void 打开ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile();
        }
        private void 打开文件toolStripButton_Click(object sender, EventArgs e)
        {
            OpenFile();
        }


        public static DashStyle GetLineStypefromString(string LineStyleIn)
        {
            DashStyle DashStyleOut = new DashStyle();

            switch (LineStyleIn)
            {
                case "Solid":
                    DashStyleOut = DashStyle.Solid;
                    break;
                case "Dash":
                    DashStyleOut = DashStyle.Dash;
                    break;
                case "DashDot":
                    DashStyleOut = DashStyle.DashDot;
                    break;
                case "DashDotDot":
                    DashStyleOut = DashStyle.DashDotDot;
                    break;
                case "Dot":
                    DashStyleOut = DashStyle.Dot;
                    break;
                default:
                    DashStyleOut = DashStyle.Solid;
                    break;
            }
            return DashStyleOut;
        }

        private void 清空toolStripButton_Click(object sender, EventArgs e)
        {
            shapes.Clear();
            dx = 0;
            dy = 0;
        }

        private void 保存toolStripButton_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Default;
            drawselect = ShapeType.Undefine;
            Isdrawing = false;
            if (toUpdate.ThreadState != ThreadState.Suspended)//如果线程没有挂起
            {
                //先挂起线程，防止保存对象被占用而出现异常，异常捕获太多了，还是要少用
                toUpdate.Suspend();
            }
            DateTime dt = DateTime.Now;
            string text = string.Format("{0:yyyyMMddHHmmssffff}", dt);
            bmp.Save("images\\" + text + ".png");
            if (toUpdate != null && toUpdate.ThreadState != ThreadState.Running)
            {
                //保存任务结束，继续运行
                toUpdate.Resume();
            }
            MessageBox.Show("已保存在 images/*.png");
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            Keys key = e.KeyCode;
            if (key == Keys.Escape)
            {
                DrawToSelect();
            }
        }




 

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void 撤销toolStripButton_Click(object sender, EventArgs e)
        {
            if (undo.Count > 0)
            {
                undo.Pop();
                redo.Push(new List<Shape>(shapes));
                shapes.Clear();
                shapes = undo.Pop();
            }

        }

        private void toolStripButton8_Click(object sender, EventArgs e)//
        {
            if (redo.Count > 0)
            {
                undo.Push(new List<Shape>(shapes));
                shapes.Clear();
                shapes = redo.Pop();

            }
        }


        float enLarge = 1.1f;//放大器
        float reduce = 0.9f;//缩小器
        private void 放大toolStripButton_Click(object sender, EventArgs e)
        {
            for(int i = 0; i < shapes.Count; i++)
            {
                for(int j = 0; j < shapes[i].points.Count; j++)
                {
                    shapes[i].points[j] = new Point(Convert.ToInt32(shapes[i].points[j].X * enLarge), Convert.ToInt32(shapes[i].points[j].Y * enLarge));
                }
                shapes[i].lineStyle.Width *= enLarge;
            }

        }

        private void 缩小toolStripButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < shapes.Count; i++)
            {
                for (int j = 0; j < shapes[i].points.Count; j++)
                {
                    shapes[i].points[j] = new Point(Convert.ToInt32(shapes[i].points[j].X * reduce), Convert.ToInt32(shapes[i].points[j].Y * reduce));
                }
                shapes[i].lineStyle.Width *= reduce;
            }
        }

        private void 平移toolStripButton_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.SizeAll;
        }






        void OpenFile()
        {
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "打开图片";
                openFileDialog.Filter = "位图文件(*.png;*.jpg)|*.png;*.jpg|矢量文件(*.lcx)|*.lcx";
                openFileDialog.ValidateNames = true; // 验证用户输入是否是一个有效的Windows文件名
                openFileDialog.CheckFileExists = true; //验证文件的有效性
                openFileDialog.CheckPathExists = true;//验证路径的有效性
                if (openFileDialog.ShowDialog() == DialogResult.OK) //用户点击确认按钮，发送确认消息
                {
                    string suffix = openFileDialog.FileName.Substring(openFileDialog.FileName.Length - 3);
                    if (suffix == "jpg" || suffix == "png")
                    {
                        Picture picture1 = new Picture();
                        shapeTypes.Add(ShapeType.Picture);
                        picture1.filename = openFileDialog.FileName;
                        picture1.points[0] = (new Point(0, 0));
                        picture1.points[1] = (new Point(pictureBox1.ClientSize));
                        pictures.Add(picture1);
                        shapes.Add(picture1);
                    }
                    else
                    {
                        FileStream fs = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);
                        StreamReader sr = new StreamReader(fs);

                        List<string> str = new List<string>(9);
                        while (sr.Peek() != -1)
                        {
                            str.Add(sr.ReadLine());
                        }

                        if (str[0] == "成都理工大学李春肖")
                        {
                            string[] s = str[2].Split(';');
                            foreach (string s2 in s)
                            {
                                if (s2.Length != 0)
                                {
                                    Line line1 = new Line();
                                    line1.Load(s2);
                                    lines.Add(line1);
                                    shapes.Add(line1);
                                }

                            }

                            s = str[3].Split(';');
                            foreach (string s3 in s)
                            {
                                if (s3.Length != 0)
                                {
                                    Rect rect1 = new Rect();
                                    rect1.Load(s3);
                                    rectangles.Add(rect1);
                                    shapes.Add(rect1);
                                }

                            }

                            s = str[4].Split(';');
                            foreach (string s4 in s)
                            {

                                if (s4.Length != 0)
                                {
                                    polygon polygon1 = new polygon();
                                    polygon1.Load(s4);
                                    polygons.Add(polygon1);
                                    shapes.Add(polygon1);
                                }

                            }

                            s = str[5].Split(';');
                            foreach (string s5 in s)
                            {
                                if (s5.Length != 0)
                                {
                                    Ellipse ellipse1 = new Ellipse();
                                    ellipse1.Load(s5);
                                    ellipses.Add(ellipse1);
                                    shapes.Add(ellipse1);
                                }

                            }

                            s = str[6].Split(';');
                            foreach (string s6 in s)
                            {
                                if (s6.Length != 0)
                                {
                                    Circle circle1 = new Circle();
                                    circle1.Load(s6);
                                    circles.Add(circle1);
                                    shapes.Add(circle1);
                                }

                            }

                            s = str[7].Split(';');
                            foreach (string s7 in s)
                            {
                                if (s7.Length != 0)
                                {
                                    Picture picture1 = new Picture();
                                    picture1.Load(s7);
                                    pictures.Add(picture1);
                                    shapes.Add(picture1);
                                }

                            }

                            s = str[8].Split(';');
                            foreach (string s8 in s)
                            {
                                if (s8.Length != 0)
                                {
                                    MyText tetx1 = new MyText();
                                    tetx1.Load(s8);
                                    texts.Add(tetx1);
                                    shapes.Add(tetx1);
                                }
                            }

                            s = str[1].Split(',');
                            foreach (string s1 in s)
                            {
                                switch (s1)
                                {
                                    case ("Line"):
                                        {
                                            shapeTypes.Add(ShapeType.Line);
                                            break;
                                        }
                                    case ("Rectangle"):
                                        {
                                            shapeTypes.Add(ShapeType.Rectangle);
                                            break;
                                        }
                                    case ("Ploygon"):
                                        {
                                            shapeTypes.Add(ShapeType.Ploygon);
                                            break;
                                        }
                                    case ("Ellipse"):
                                        {
                                            shapeTypes.Add(ShapeType.Ellipse);
                                            break;
                                        }
                                    case ("Circle"):
                                        {
                                            shapeTypes.Add(ShapeType.Circle);
                                            break;
                                        }
                                    case ("Text"):
                                        {
                                            shapeTypes.Add(ShapeType.Text);
                                            break;
                                        }
                                    case ("Picture"):
                                        {
                                            shapeTypes.Add(ShapeType.Picture);
                                            break;
                                        }
                                }
                            }
                        }

                    }
                    openFileDialog.Dispose();
                    strFileName = null;
                }
            }
        }


        void DrawToSelect()
        {
            tempPoints.Clear();
            this.Cursor = Cursors.Default;
            drawselect = ShapeType.Undefine;
            Isdrawing = false;
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (this.Cursor == Cursors.Default)
            {
                Rectangle rect = new Rectangle(e.X-2, e.Y-2, 4, 4);
                for (int i = 0; i < shapes.Count; i++)
                {
                    Shape shape = shapes[i];
                    if (shape.IsContain(rect))
                    {
                        shape.lineStyle.Width = 5;
                        shape.lineStyle.Color = Color.Red;
                    }
                }
            }

        }

        private void 线ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Isdrawing = false;
            drawselect = ShapeType.Line;//图形选择为直线
            this.Cursor = Cursors.Cross;//鼠标光标变为“十”字
        }

        private void 矩形ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Isdrawing = false;
            drawselect = ShapeType.Rectangle;//图形选择为矩形
            this.Cursor = Cursors.Cross;//鼠标光标变为“十”字
        }

        private void duobianxingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Isdrawing = false;
            drawselect = ShapeType.Ploygon;//图形选择为多边形
            this.Cursor = Cursors.Cross;//鼠标光标变为“十”字
            IsPloygon = true;
        }

        private void 椭圆ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Isdrawing = false;
            drawselect = ShapeType.Circle;//图形选择为圆
            this.Cursor = Cursors.Cross;//鼠标光标变为“十”字
        }

        private void 多边形ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Isdrawing = false;
            drawselect = ShapeType.Ellipse;//图形选择为椭圆
            this.Cursor = Cursors.Cross;//鼠标光标变为“十”字
        }

        private void 图片ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Isdrawing = false;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "打开图片";
            ofd.Filter = "位图文件(*.png;*.jpg)|*.png;*.jpg|矢量文件(*.shp;*.svg)|*.shp;*.svg";
            ofd.ValidateNames = true; // 验证用户输入是否是一个有效的Windows文件名
            ofd.CheckFileExists = true; //验证文件的有效性
            ofd.CheckPathExists = true;//验证路径的有效性
            if (ofd.ShowDialog() == DialogResult.OK) //用户点击确认按钮，发送确认消息
            {
                strFileName = ofd.FileName;//获取在文件对话框中选定的路径或者字符串
                drawselect = ShapeType.Picture;//图形选择为图像
                this.Cursor = Cursors.Cross;//鼠标光标变为“十”字

                ofd.Dispose();
                //strFileName = null;
            }
        }

        private void 文本ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Isdrawing = false;
            FontDialog fontDialog = new FontDialog();//新建一个字体对话框
            fontDialog.AllowScriptChange = true;
            fontDialog.ShowColor = true;
            var result = fontDialog.ShowDialog();//打开颜色对话框，并接收对话框操作结果
            if (result == DialogResult.OK)//如果用户点击OK
            {
                var font = fontDialog.Font;//获取用户选择的颜色
                var fontColor = fontDialog.Color;
                线宽toolStripComboBox.Font = Font;
                线宽toolStripComboBox.ForeColor = fontColor;
                drawselect = ShapeType.Text;//图形选择为文本
                this.Cursor = Cursors.Cross;//鼠标光标变为“十”字

                fontDialog.Dispose();
            }
        }

        private void 放大ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < shapes.Count; i++)
            {
                for (int j = 0; j < shapes[i].points.Count; j++)
                {
                    shapes[i].points[j] = new Point(Convert.ToInt32(shapes[i].points[j].X * enLarge), Convert.ToInt32(shapes[i].points[j].Y * enLarge));
                }
                shapes[i].lineStyle.Width *= enLarge;
            }
        }

        private void 缩小ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < shapes.Count; i++)
            {
                for (int j = 0; j < shapes[i].points.Count; j++)
                {
                    shapes[i].points[j] = new Point(Convert.ToInt32(shapes[i].points[j].X * reduce), Convert.ToInt32(shapes[i].points[j].Y * reduce));
                }
                shapes[i].lineStyle.Width *= reduce;
            }
        }

        private void 平移ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.SizeAll;
        }

        private void 删除全部ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            shapes.Clear();
            dx = 0;
            dy = 0;
        }

        private void 撤销ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (undo.Count > 0)
            {
                undo.Pop();
                redo.Push(new List<Shape>(shapes));
                shapes.Clear();
                shapes = undo.Pop();
            }
        }

        private void 重做ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (redo.Count > 0)
            {
                undo.Push(new List<Shape>(shapes));
                shapes.Clear();
                shapes = redo.Pop();

            }
        }

        private void 新建ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            shapes.Clear();
            dx = 0;
            dy = 0;
        }

        private void 帮助ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"
Draw是一个漂亮的小工具，无论您是想对栅格图像还是矢量文件进行简单的调整，
Draw都非常有能力进行编辑和改进图像：
1.绘制图像时，直接在想要绘制的地方点击鼠标左键即可绘制直线和多边形，其他图形需要按住左键不放进行拖拽；
2.您还可以选择线条颜色和图形填充颜色；
2.鼠标双击、右键、键盘esc键都可以退出绘制模式到选择模式，点击图形即可选中（只实现了直线的选择、未完善）；
3.点击移动会进入移动模式，此时在画布上按住左键进行拖拽会移动图形（有bug，此功能我是直接用的画布的移动，移动后画出的图形将不在鼠标位置）；
4.矢量文件是我自定义的*.lcx文件，对于其它矢量格式并不兼容；");
        }

        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"
1.此程序是我的数据结构作业，因本人能力不足，很多功能并未实现或完善：如单选或框选图形、右键编辑、顺序调整等；
2.此程序所有图标均来自于：https://www.iconfont.cn/和https://icons8.com。；");
        }
    }













}
