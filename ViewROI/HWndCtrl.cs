using System;
using ViewROI;
using System.Collections;
using HalconDotNet;
using System.Drawing;
using System.Data.Odbc;

namespace ViewROI
{
	public delegate void IconicDelegate(int val);//图形委托
	public delegate void FuncDelegate();

	/// <summary>
	/// This class works as a wrapper class for the HALCON window
	/// HWindow. HWndCtrl is in charge of the visualization.
	/// You can move and zoom the visible image part by using GUI component 
	/// inputs or with the mouse. The class HWndCtrl uses a graphics stack 
	/// to manage the iconic objects for the display. Each object is linked 
	/// to a graphical context, which determines how the object is to be drawn.
	/// The context can be changed by calling changeGraphicSettings().
	/// The graphical "modes" are defined by the class GraphicsContext and 
	/// map most of the dev_set_* operators provided in HDevelop.
	/// </summary>
	public class HWndCtrl
	{
		/// <summary>No action is performed on mouse events  鼠标事件后没有需要执行的函数</summary>
		public const int MODE_VIEW_NONE       = 10;
		/// <summary>Zoom is performed on mouse events   鼠标事件后执行缩放任务</summary>
		public const int MODE_VIEW_ZOOM       = 11;
		/// <summary>Move is performed on mouse events   鼠标事件后执行移动任务</summary>
		public const int MODE_VIEW_MOVE       = 12;
		/// <summary>Magnification is performed on mouse events  鼠标事件后执行放大任务</summary>
		public const int MODE_VIEW_ZOOMWINDOW	= 13;
        /// <summary> 包含ROI </summary>
		public const int MODE_INCLUDE_ROI     = 1;
        /// <summary> 排除ROI </summary>
		public const int MODE_EXCLUDE_ROI     = 2;
		/// <summary>
		/// Constant describes delegate message to signal new image  用来描述委托的新图片的信息
		/// </summary>
		public const int EVENT_UPDATE_IMAGE   = 31;
		/// <summary>
		/// Constant describes delegate message to signal error      
		/// when reading an image from file
		/// </summary>
		public const int ERR_READING_IMG      = 32;
		/// <summary> 
		/// Constant describes delegate message to signal error
		/// when defining a graphical context
		/// </summary>
		public const int ERR_DEFINING_GC      = 33;
		/// <summary> 
		/// Maximum number of HALCON objects that can be put on the graphics   最大的Halcon对象数量
		/// stack without loss. For each additional object, the first entry 
		/// is removed from the stack again.
		/// </summary>
		private const int MAXNUMOBJLIST       = 50;
		private int    stateView;
		private bool   mousePressed = false;    //鼠标按钮变量
		private double startX,startY;
		/// <summary>HALCON window</summary>
		private HWindowControl viewPort;
		/// <summary>
		/// Instance of ROIController, which manages ROI interaction
		/// </summary>
		private ROIController roiManager;   
		/* dispROI is a flag to know when to add the ROI models to the 
		   paint routine and whether or not to respond to mouse events for 
		   ROI objects */
		private int           dispROI;
		/* Basic parameters, like dimension of window and displayed image part    窗口的参数，窗口的尺寸，显示图片的尺寸 */
		private int   windowWidth;
		private int   windowHeight;
		private int   imageWidth;
		private int   imageHeight;
		private int[] CompRangeX;
		private int[] CompRangeY;
		private int    prevCompX, prevCompY;
		private double stepSizeX, stepSizeY;
		/* Image coordinates, which describe the image part that is displayed  
		   in the HALCON window     图像显示的起点终点坐标*/
		private double ImgRow1, ImgCol1, ImgRow2, ImgCol2;
		/// <summary>Error message when an exception is thrown    异常信息</summary>
		public string  exceptionText = "";
		/* Delegates to send notification messages to other classes */
		/// <summary>
		/// Delegate to add information to the HALCON window after 
		/// the paint routine has finished
		/// </summary>
		public FuncDelegate   addInfoDelegate;
		/// <summary>
		/// Delegate to notify about failed tasks of the HWndCtrl instance
		/// </summary>
		public IconicDelegate NotifyIconObserver;   //图形委托
		public HMouseEventHandler HMouseClick;
		private HWindow ZoomWindow;
		private double  zoomWndFactor;
		private double  zoomAddOn;
		private int     zoomWndSize;

		/// <summary> 
		/// List of HALCON objects to be drawn into the HALCON window. 
		/// The list shouldn't contain more than MAXNUMOBJLIST objects, 
		/// otherwise the first entry is removed from the list.
		/// </summary>
		private ArrayList HObjList;
		/// <summary>
		/// Instance that describes the graphical context for the
		/// HALCON window. According on the graphical settings
		/// attached to each HALCON object, this graphical context list 
		/// is updated constantly.
		/// </summary>
		private GraphicsContext	mGC;


		/// <summary> 
		/// Initializes the image dimension, mouse delegation, and the  初始化图像尺寸
		/// graphical context setup of the instance.
		/// </summary>
		/// <param name="view"> HALCON window </param>
		public HWndCtrl(HWindowControl view)
		{
			viewPort = view;
			stateView = MODE_VIEW_NONE;
			windowWidth = viewPort.Size.Width;
			windowHeight = viewPort.Size.Height;

			zoomWndFactor = (double)imageWidth / viewPort.Width;
			zoomAddOn = Math.Pow(0.9, 5);
			zoomWndSize = 150;

			/*default*/
			CompRangeX = new int[] { 0, 100 };
			CompRangeY = new int[] { 0, 100 };

			prevCompX = prevCompY = 0;

			dispROI = MODE_INCLUDE_ROI;//1;

			viewPort.HMouseUp += new HalconDotNet.HMouseEventHandler(this.mouseUp);
			viewPort.HMouseDown += new HalconDotNet.HMouseEventHandler(this.mouseDown);
			viewPort.HMouseMove += new HalconDotNet.HMouseEventHandler(this.mouseMoved);
            viewPort.HMouseWheel += new HalconDotNet.HMouseEventHandler(this.HMouseWheel);
			addInfoDelegate = new FuncDelegate(dummyV);
			NotifyIconObserver = new IconicDelegate(dummy);

			// graphical stack 
			HObjList = new ArrayList(20);
			mGC = new GraphicsContext();
			mGC.gcNotification = new GCDelegate(exceptionGC);
			viewPort.HMouseDown += new HalconDotNet.HMouseEventHandler(this.mouseClick);
		}

		

		private void mouseClick(object sender, HalconDotNet.HMouseEventArgs e)
		{
			if(HMouseClick != null){
				this.HMouseClick(sender, e);			
			}	
		}

		/// <summary>
		/// Registers an instance of an ROIController with this window 
		/// controller (and vice versa).
		/// </summary>
		/// <param name="rC"> 
		/// Controller that manages interactive ROIs for the HALCON window 
		/// </param>
		public void useROIController(ROIController rC){
			roiManager = rC;
			rC.setViewController(this);
		}


		/// <summary>
		/// Read dimensions of the image to adjust own window settings
		/// </summary>
		/// <param name="image">HALCON image</param>
		private void setImagePart(HImage image){
			string s;
			int w,h;
			image.GetImagePointer1(out s, out w, out h);
			setImagePart(0, 0, h, w);
		}


		/// <summary>
		/// Adjust window settings by the values supplied for the left 
		/// upper corner and the right lower corner
		/// </summary>
		/// <param name="r1">y coordinate of left upper corner</param>
		/// <param name="c1">x coordinate of left upper corner</param>
		/// <param name="r2">y coordinate of right lower corner</param>
		/// <param name="c2">x coordinate of right lower corner</param>
		private void setImagePart(int r1, int c1, int r2, int c2){
			ImgRow1 = r1;
			ImgCol1 = c1;
			ImgRow2 = imageHeight = r2;
			ImgCol2 = imageWidth = c2;
			System.Drawing.Rectangle rect = viewPort.ImagePart;
			rect.X = (int)ImgCol1;
			rect.Y = (int)ImgRow1;
			rect.Height = (int)imageHeight;
			rect.Width = (int)imageWidth;
			viewPort.ImagePart = rect;
		}


		/// <summary>
		/// Sets the view mode for mouse events in the HALCON window
		/// (zoom, move, magnify or none).
		/// </summary>
		/// <param name="mode">One of the MODE_VIEW_* constants</param>
		public void setViewState(int mode){
			stateView = mode;
			if (roiManager != null)
				roiManager.resetROI();
		}

		/********************************************************************/
		private void dummy(int val)
		{
		}

		private void dummyV()
		{
		}

		/*******************************************************************/
		private void exceptionGC(string message)
		{
			exceptionText = message;
			NotifyIconObserver(ERR_DEFINING_GC);
		}

		/// <summary>
		/// Paint or don't paint the ROIs into the HALCON window by 
		/// defining the parameter to be equal to 1 or not equal to 1.
		/// </summary>
		public void setDispLevel(int mode)
		{
			dispROI = mode;
		}

		/****************************************************************************/
		/*                          graphical element                               */
		/****************************************************************************/
		private void zoomImage(double x, double y, double scale) // 显示缩放
		{
			double lengthC, lengthR;
			double percentC, percentR;
			int    lenC, lenR;

			percentC = (x - ImgCol1) / (ImgCol2 - ImgCol1);
			percentR = (y - ImgRow1) / (ImgRow2 - ImgRow1);

			lengthC = (ImgCol2 - ImgCol1) * scale;
			lengthR = (ImgRow2 - ImgRow1) * scale;

			ImgCol1 = x - lengthC * percentC;
			ImgCol2 = x + lengthC * (1 - percentC);

			ImgRow1 = y - lengthR * percentR;
			ImgRow2 = y + lengthR * (1 - percentR);

			lenC = (int)Math.Round(lengthC);
			lenR = (int)Math.Round(lengthR);

			System.Drawing.Rectangle rect = viewPort.ImagePart;
			rect.X = (int)Math.Round(ImgCol1);
			rect.Y = (int)Math.Round(ImgRow1);
			rect.Width = (lenC > 0) ? lenC : 1;
			rect.Height = (lenR > 0) ? lenR : 1;
			viewPort.ImagePart = rect;

			zoomWndFactor *= scale;
			repaint();
		}

		/// <summary>
		/// Scales the image in the HALCON window according to the 
		/// value scaleFactor
		/// </summary>
		public void zoomImage(double scaleFactor)
		{
			double midPointX, midPointY;

			if (((ImgRow2 - ImgRow1) == scaleFactor * imageHeight) &&
				((ImgCol2 - ImgCol1) == scaleFactor * imageWidth))
			{
				repaint();
				return;
			}

			ImgRow2 = ImgRow1 + imageHeight;
			ImgCol2 = ImgCol1 + imageWidth;

			midPointX = ImgCol1;
			midPointY = ImgRow1;

			zoomWndFactor = (double)imageWidth / viewPort.Width;
			zoomImage(midPointX, midPointY, scaleFactor);
		}


		/// <summary>
		/// Scales the HALCON window according to the value scale
		/// </summary>
		public void scaleWindow(double scale)
		{
			ImgRow1 = 0;
			ImgCol1 = 0;

			ImgRow2 = imageHeight;
			ImgCol2 = imageWidth;

			viewPort.Width = (int)(ImgCol2 * scale);
			viewPort.Height = (int)(ImgRow2 * scale);

			zoomWndFactor = ((double)imageWidth / viewPort.Width);
		}

		/// <summary>
		/// Recalculates the image-window-factor, which needs to be added to 
		/// the scale factor for zooming an image. This way the zoom gets 
		/// adjusted to the window-image relation, expressed by the equation 
		/// imageWidth/viewPort.Width.
		/// </summary>
		public void setZoomWndFactor()
		{
			zoomWndFactor = ((double)imageWidth / viewPort.Width);
		}

		/// <summary>
		/// Sets the image-window-factor to the value zoomF
		/// </summary>
		public void setZoomWndFactor(double zoomF)
		{
			zoomWndFactor = zoomF;
		}

		/*******************************************************************/
		private void moveImage(double motionX, double motionY)
		{
			ImgRow1 += -motionY;
			ImgRow2 += -motionY;

			ImgCol1 += -motionX;
			ImgCol2 += -motionX;

			System.Drawing.Rectangle rect = viewPort.ImagePart;
			rect.X = (int)Math.Round(ImgCol1);
			rect.Y = (int)Math.Round(ImgRow1);
			viewPort.ImagePart = rect;

			repaint();
		}


		/// <summary>
		/// Resets all parameters that concern the HALCON window display 
		/// setup to their initial values and clears the ROI list.
		/// </summary>
		public void resetAll()
		{
			ImgRow1 = 0;
			ImgCol1 = 0;
			ImgRow2 = imageHeight;
			ImgCol2 = imageWidth;

			zoomWndFactor = (double)imageWidth / viewPort.Width;

			System.Drawing.Rectangle rect = viewPort.ImagePart;
			rect.X = (int)ImgCol1;
			rect.Y = (int)ImgRow1;
			rect.Width = (int)imageWidth;
			rect.Height = (int)imageHeight;
			viewPort.ImagePart = rect;


			if (roiManager != null)
				roiManager.reset();
		}

		public void resetWindow()
		{
			ImgRow1 = 0;
			ImgCol1 = 0;
			ImgRow2 = imageHeight;
			ImgCol2 = imageWidth;

			zoomWndFactor = (double)imageWidth / viewPort.Width;

			System.Drawing.Rectangle rect = viewPort.ImagePart;
			rect.X = (int)ImgCol1;
			rect.Y = (int)ImgRow1;
			rect.Width = (int)imageWidth;
			rect.Height = (int)imageHeight;
			viewPort.ImagePart = rect;
		}


		/*************************************************************************/
		/*      			 Event handling for mouse	   	                     */
		/*************************************************************************/
		private void mouseDown(object sender, HalconDotNet.HMouseEventArgs e)
		{
			mousePressed = true;
			int activeROIidx = -1;
			double scale;

			if (roiManager != null && (dispROI == MODE_INCLUDE_ROI))
			{
				activeROIidx = roiManager.mouseDownAction(e.X, e.Y); //规避感兴趣区域
			}

			if (activeROIidx == -1)
			{
				switch (stateView)
				{
					case MODE_VIEW_MOVE:
						startX = e.X;
						startY = e.Y;
						break;
					case MODE_VIEW_ZOOM:
						if (e.Button == System.Windows.Forms.MouseButtons.Left)
							scale = 0.9;
						else
							scale = 1 / 0.9;
						zoomImage(e.X, e.Y, scale);
						break;
					case MODE_VIEW_NONE:
						break;
					case MODE_VIEW_ZOOMWINDOW:
						activateZoomWindow((int)e.X, (int)e.Y);
						break;
					default:
						break;
				}
			}
			//end of if
		}


        private void HMouseWheel(object sender, HMouseEventArgs e)
        {
            double scale =1;
            if (e.Delta <= 0)
            {
                scale = 0.9;
            }
            else if (e.Delta > 0)
            {
                scale = 1 / 0.9;
            }
            try
            {
                zoomImage(e.X, e.Y, scale);
            }
            catch { }
        }



		/*******************************************************************/
		private void activateZoomWindow(int X, int Y)
		{
			double posX, posY;
			int zoomZone;

			if (ZoomWindow != null)
				ZoomWindow.Dispose();

			HOperatorSet.SetSystem("border_width", 10);
			ZoomWindow = new HWindow();

			posX = ((X - ImgCol1) / (ImgCol2 - ImgCol1)) * viewPort.Width;
			posY = ((Y - ImgRow1) / (ImgRow2 - ImgRow1)) * viewPort.Height;

			zoomZone = (int)((zoomWndSize / 2) * zoomWndFactor * zoomAddOn);
			ZoomWindow.OpenWindow((int)posY - (zoomWndSize / 2), (int)posX - (zoomWndSize / 2),
								   zoomWndSize, zoomWndSize,
								   viewPort.HalconID, "visible", "");
			ZoomWindow.SetPart(Y - zoomZone, X - zoomZone, Y + zoomZone, X + zoomZone);
			repaint(ZoomWindow);
			ZoomWindow.SetColor("black");
		}

		/*******************************************************************/
		private void mouseUp(object sender, HalconDotNet.HMouseEventArgs e)
		{
			mousePressed = false;

			if (roiManager != null
				&& (roiManager.activeROIidx != -1)
				&& (dispROI == MODE_INCLUDE_ROI))
			{
				roiManager.NotifyRCObserver(ROIController.EVENT_UPDATE_ROI);
			}
			else if (stateView == MODE_VIEW_ZOOMWINDOW)
			{
				ZoomWindow.Dispose();
			}
		}






		/*******************************************************************/
		private void mouseMoved(object sender, HalconDotNet.HMouseEventArgs e)
		{
			double motionX, motionY;
			double posX, posY;
			double zoomZone;
            //if (viewPort.)
            int count = HObjList.Count;
            foreach (var item in HObjList)  //实时显示图片的灰度  灰度显示
            {
                HObjectEntry objNow = (HObjectEntry)item;
                if (objNow.HObj is HImage) {
					try{
						HObject myImg = new HObject();
						if (!objNow.HObj.IsInitialized()) break;
						//HOperatorSet.CopyObj(objNow.HObj, out myImg, 1, -1);
						HTuple Gray = new HTuple();
						HTuple Wid, Hei;
						HOperatorSet.GetImageSize(objNow.HObj, out Wid, out Hei);
						if (0 < e.Y && e.Y < Hei && 0 < e.X && e.X < Wid){
							repaint(viewPort.HalconWindow);
							HOperatorSet.GetGrayval(objNow.HObj, e.Y, e.X, out Gray);
							double scale = (ImgRow2 - ImgRow1) / Hei.D;
							//HOperatorSet.SetTposition(viewPort.HalconWindow, ImgRow2 - 400 * scale, ImgCol2 - 400 * scale);
							HOperatorSet.SetTposition(viewPort.HalconWindow, e.Y+20* scale, e.X+50* scale);
							HOperatorSet.SetColor(viewPort.HalconWindow, "red");
							HOperatorSet.WriteString(viewPort.HalconWindow, "R:" + ((int)e.Y).ToString() + " " + "C:" + 
								((int)e.X).ToString() + "  " + ((int)Gray[0].D).ToString());
						}
						//myImg.Dispose();
						break;
					}
					catch{
						break;
					}      
                }                
            }
			if (!mousePressed)
				return;
			if (roiManager != null && (roiManager.activeROIidx != -1) && (dispROI == MODE_INCLUDE_ROI)){
				roiManager.mouseMoveAction(e.X, e.Y);
			}
			else if (stateView == MODE_VIEW_MOVE)  //平移视图
			{
				motionX = ((e.X - startX));
				motionY = ((e.Y - startY));

				if (((int)motionX != 0) || ((int)motionY != 0))
				{
					moveImage(motionX, motionY);
					startX = e.X - motionX;
					startY = e.Y - motionY;
				}
			}
			else if (stateView == MODE_VIEW_ZOOMWINDOW)  //缩放视图
			{
				HSystem.SetSystem("flush_graphic", "false");
				ZoomWindow.ClearWindow();

				posX = ((e.X - ImgCol1) / (ImgCol2 - ImgCol1)) * viewPort.Width;
				posY = ((e.Y - ImgRow1) / (ImgRow2 - ImgRow1)) * viewPort.Height;
				zoomZone = (zoomWndSize / 2) * zoomWndFactor * zoomAddOn;

				ZoomWindow.SetWindowExtents((int)posY - (zoomWndSize / 2),
											(int)posX - (zoomWndSize / 2),
											zoomWndSize, zoomWndSize);
				ZoomWindow.SetPart((int)(e.Y - zoomZone), (int)(e.X - zoomZone),
								   (int)(e.Y + zoomZone), (int)(e.X + zoomZone));
				repaint(ZoomWindow);

				HSystem.SetSystem("flush_graphic", "true");
				ZoomWindow.DispLine(-100.0, -100.0, -100.0, -100.0);
			}
		}

		public void writeString( int x ,int y, string  colorIn,string StrValue )
		{
			double scale = (ImgRow2 - ImgRow1) / imageHeight;
			HOperatorSet.SetTposition(viewPort.HalconWindow, ImgRow1+( y)* scale, ImgCol1+(x) * scale); 
			HOperatorSet.SetColor(viewPort.HalconWindow, colorIn);
			HOperatorSet.WriteString(viewPort.HalconWindow, StrValue);
		}


		public void DrawRect1(out int Row1,out int Col1 ,out int Row2,out int Col2)
		{
			viewPort.HMouseUp -= new HalconDotNet.HMouseEventHandler(this.mouseUp);
			viewPort.HMouseDown -= new HalconDotNet.HMouseEventHandler(this.mouseDown);
			viewPort.HMouseMove -= new HalconDotNet.HMouseEventHandler(this.mouseMoved);
			viewPort.HMouseWheel -= new HalconDotNet.HMouseEventHandler(this.HMouseWheel);
			Row1 = 0;
			Col1 = 0;
			Row2 = 0;
			Col2 = 0;
			//roiManager.
			HTuple row1, col1, row2, col2;
			HOperatorSet.DrawRectangle1(viewPort.HalconWindow, out row1, out col1, out row2, out col2);
            Row1 = (int)row1.D;
			Col1 = (int)col1.D;
			Row2 = (int)row2.D;
			Col2 = (int)col2.D;
			viewPort.HMouseUp += new HalconDotNet.HMouseEventHandler(this.mouseUp);
			viewPort.HMouseDown += new HalconDotNet.HMouseEventHandler(this.mouseDown);
			viewPort.HMouseMove += new HalconDotNet.HMouseEventHandler(this.mouseMoved);
			viewPort.HMouseWheel += new HalconDotNet.HMouseEventHandler(this.HMouseWheel);
		}
		/// <summary>
		/// To initialize the move function using a GUI component, the HWndCtrl
		/// first needs to know the range supplied by the GUI component. 
		/// For the x direction it is specified by xRange, which is 
		/// calculated as follows: GuiComponentX.Max()-GuiComponentX.Min().
		/// The starting value of the GUI component has to be supplied 
		/// by the parameter Init
		/// </summary>
		public void setGUICompRangeX(int[] xRange, int Init)
		{
			int cRangeX;

			CompRangeX = xRange;
			cRangeX = xRange[1] - xRange[0];
			prevCompX = Init;
			stepSizeX = ((double)imageWidth / cRangeX) * (imageWidth / windowWidth);

		}

		/// <summary>
		/// To initialize the move function using a GUI component, the HWndCtrl
		/// first needs to know the range supplied by the GUI component. 
		/// For the y direction it is specified by yRange, which is 
		/// calculated as follows: GuiComponentY.Max()-GuiComponentY.Min().
		/// The starting value of the GUI component has to be supplied 
		/// by the parameter Init
		/// </summary>
		public void setGUICompRangeY(int[] yRange, int Init)
		{
			int cRangeY;

			CompRangeY = yRange;
			cRangeY = yRange[1] - yRange[0];
			prevCompY = Init;
			stepSizeY = ((double)imageHeight / cRangeY) * (imageHeight / windowHeight);
		}


		/// <summary>
		/// Resets to the starting value of the GUI component.
		/// </summary>
		public void resetGUIInitValues(int xVal, int yVal)
		{
			prevCompX = xVal;
			prevCompY = yVal;
		}

		/// <summary>
		/// Moves the image by the value valX supplied by the GUI component
		/// </summary>
		public void moveXByGUIHandle(int valX)
		{
			double motionX;

			motionX = (valX - prevCompX) * stepSizeX;

			if (motionX == 0)
				return;

			moveImage(motionX, 0.0);
			prevCompX = valX;
		}


		/// <summary>
		/// Moves the image by the value valY supplied by the GUI component
		/// </summary>
		public void moveYByGUIHandle(int valY)
		{
			double motionY;
			motionY = (valY - prevCompY) * stepSizeY;
			if (motionY == 0) return;
			moveImage(0.0, motionY);
			prevCompY = valY;
		}

		/// <summary>
		/// Zooms the image by the value valF supplied by the GUI component
		/// </summary>
		public void zoomByGUIHandle(double valF)
		{
			double x, y, scale;
			double prevScaleC;

			x = (ImgCol1 + (ImgCol2 - ImgCol1) / 2);
			y = (ImgRow1 + (ImgRow2 - ImgRow1) / 2);

			prevScaleC = (double)((ImgCol2 - ImgCol1) / imageWidth);
			scale = ((double)1.0 / prevScaleC * (100.0 / valF));

			zoomImage(x, y, scale);
		}

		/// <summary>
		/// Triggers a repaint of the HALCON window
		/// </summary>
		public void repaint()
		{
			repaint(viewPort.HalconWindow);
		}

		/// <summary>
		/// Repaints the HALCON window 'window'
		/// </summary>
		public void repaint(HalconDotNet.HWindow window)
		{
			int count = HObjList.Count;
			HObjectEntry entry;

			HSystem.SetSystem("flush_graphic", "false");
			window.ClearWindow();
			mGC.stateOfSettings.Clear();

			for (int i=0; i < count; i++)
			{
                try
                {
                    entry = ((HObjectEntry)HObjList[i]);
                    mGC.applyContext(window, entry.gContext);
					if(entry.HObj.IsInitialized())
                    window.DispObj(entry.HObj);

                }
                catch
                { }

			}

			addInfoDelegate();

			if (roiManager != null && (dispROI == MODE_INCLUDE_ROI))
				roiManager.paintData(window);

			HSystem.SetSystem("flush_graphic", "true");

			window.SetColor("black");
			window.DispLine(-100.0, -100.0, -101.0, -101.0);
		}



		/********************************************************************/
		/*                      GRAPHICSSTACK                               */
		/********************************************************************/

		/// <summary>
		/// Adds an iconic object to the graphics stack similar to the way
		/// it is defined for the HDevelop graphics stack.
		/// </summary>
		/// <param name="obj">Iconic object</param>
		public void addIconicVar(HObject obj)
		{
			HObjectEntry entry;
			if (obj == null) return;
			if (obj is HImage)
			{
				double r, c;
				int h, w, area;
				string s;
				viewPort.HMouseUp -= new HalconDotNet.HMouseEventHandler(this.mouseUp);
				viewPort.HMouseDown -= new HalconDotNet.HMouseEventHandler(this.mouseDown);
				viewPort.HMouseMove -= new HalconDotNet.HMouseEventHandler(this.mouseMoved);
				viewPort.HMouseWheel -= new HalconDotNet.HMouseEventHandler(this.HMouseWheel);
				area = ((HImage)obj).GetDomain().AreaCenter(out r, out c);
				((HImage)obj).GetImagePointer1(out s, out w, out h);
				//if (area == (w * h))
				//{
					clearList();
					if ((h != imageHeight) || (w != imageWidth)){
						imageHeight = h;
						imageWidth = w;
						zoomWndFactor = (double)imageWidth / viewPort.Width;
						Size WindSize = viewPort.Size;
						double WindC = ((double)WindSize.Height) /((double) WindSize.Width) ;
						double ImgC = ((double) imageHeight)/ ((double)imageWidth) ;					
						if (WindC > ImgC){
							h = (int)(w * WindC);
							setImagePart(0-(h- imageHeight)/2, 0, h - (h - imageHeight) / 2, w);							
						}
						else{
							w = (int)(h / WindC);
							setImagePart(0, 0, h, w);
						}
						zoomImage(imageHeight / 2, imageHeight / 2, 1);
					}
				//}//if
				viewPort.HMouseUp += new HalconDotNet.HMouseEventHandler(this.mouseUp);
				viewPort.HMouseDown += new HalconDotNet.HMouseEventHandler(this.mouseDown);
				viewPort.HMouseMove += new HalconDotNet.HMouseEventHandler(this.mouseMoved);
				viewPort.HMouseWheel += new HalconDotNet.HMouseEventHandler(this.HMouseWheel);
			}//if
			entry = new HObjectEntry(obj, mGC.copyContextList());  //
			HObjList.Add(entry);
			if (HObjList.Count > MAXNUMOBJLIST)    HObjList.RemoveAt(1);
		}

		private bool IsFirst = true;
		/// <summary>
		/// 显示鼠标点中的瑕疵
		/// </summary>
		public void addIconicVarShow(HObject obj){
			if (obj is HImage) return;
			if (HObjList == null) return;
			if (HObjList.Count == 0) return;
			HObjectEntry entry;
			entry = new HObjectEntry(obj, mGC.copyContextList());		
			if (IsFirst){
				IsFirst = false;							
			}
			else{
				HObjList.RemoveAt(HObjList.Count-1);
			}
			HObjList.Add(entry);
		}

		public void addImg(HObject obj)
		{
			HObjectEntry entry;
			if (obj == null)  return;
			entry = new HObjectEntry(obj, mGC.copyContextList());  //
			HObjList.Add(entry);
			if (HObjList.Count > MAXNUMOBJLIST) HObjList.RemoveAt(1);
		}
		/// <summary>
		/// Clears all entries from the graphics stack 
		/// </summary>
		public void clearList()
		{
			this.IsFirst = true;
			HObjList.Clear();
		}

		/// <summary>
		/// Returns the number of items on the graphics stack
		/// </summary>
		public int getListCount()
		{
			return HObjList.Count;
		}

		/// <summary>
		/// Changes the current graphical context by setting the specified mode
		/// (constant starting by GC_*) to the specified value.
		/// </summary>
		/// <param name="mode">
		/// Constant that is provided by the class GraphicsContext
		/// and describes the mode that has to be changed, 
		/// e.g., GraphicsContext.GC_COLOR
		/// </param>
		/// <param name="val">
		/// Value, provided as a string, 
		/// the mode is to be changed to, e.g., "blue" 
		/// </param>
		public void changeGraphicSettings(string mode, string val)
		{
			switch (mode)
			{
				case GraphicsContext.GC_COLOR:
					mGC.setColorAttribute(val);
					break;
				case GraphicsContext.GC_DRAWMODE:
					mGC.setDrawModeAttribute(val);
					break;
				case GraphicsContext.GC_LUT:
					mGC.setLutAttribute(val);
					break;
				case GraphicsContext.GC_PAINT:
					mGC.setPaintAttribute(val);
					break;
				case GraphicsContext.GC_SHAPE:
					mGC.setShapeAttribute(val);
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Changes the current graphical context by setting the specified mode
		/// (constant starting by GC_*) to the specified value.
		/// </summary>
		/// <param name="mode">
		/// Constant that is provided by the class GraphicsContext
		/// and describes the mode that has to be changed, 
		/// e.g., GraphicsContext.GC_LINEWIDTH
		/// </param>
		/// <param name="val">
		/// Value, provided as an integer, the mode is to be changed to, 
		/// e.g., 5 
		/// </param>
		public void changeGraphicSettings(string mode, int val)
		{
			switch (mode){
				case GraphicsContext.GC_COLORED:
					mGC.setColoredAttribute(val);
					break;
				case GraphicsContext.GC_LINEWIDTH:
					mGC.setLineWidthAttribute(val);
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Changes the current graphical context by setting the specified mode
		/// (constant starting by GC_*) to the specified value.
		/// </summary>
		/// <param name="mode">
		/// Constant that is provided by the class GraphicsContext
		/// and describes the mode that has to be changed, 
		/// e.g.,  GraphicsContext.GC_LINESTYLE
		/// </param>
		/// <param name="val">
		/// Value, provided as an HTuple instance, the mode is 
		/// to be changed to, e.g., new HTuple(new int[]{2,2})
		/// </param>
		public void changeGraphicSettings(string mode, HTuple val)
		{
			switch (mode)
			{
				case GraphicsContext.GC_LINESTYLE:
					mGC.setLineStyleAttribute(val);
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Clears all entries from the graphical context list
		/// </summary>
		public void clearGraphicContext()
		{
			mGC.clear();
		}

		/// <summary>
		/// Returns a clone of the graphical context list (hashtable)
		/// </summary>
		public Hashtable getGraphicContext()
		{
			return mGC.copyContextList();
		}


		public HWindowControl GetWindow()
		{
			return viewPort;
		}

	}//end of class
}//end of namespace
