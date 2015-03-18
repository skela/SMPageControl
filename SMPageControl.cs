using System;
using System.Collections.Generic;
using CoreGraphics;

using UIKit;
using CoreGraphics;
using Foundation;

namespace SMPageControlLib
{
	public enum SMPageControlAlignment
	{
		Left = 1,
		Center,
		Right
	};

	public enum SMPageControlVerticalAlignment
	{
		Top = 1,
		Middle,
		Bottom
	};

	[Register("SMPageControl")]
	public class SMPageControl : UIControl
	{
		int numberOfPages;
		int currentPage;
		float indicatorMargin;
		float indicatorDiameter;
		SMPageControlAlignment alignment;
		SMPageControlVerticalAlignment verticalAlignment;

		UIImage pageIndicatorImage;
		UIImage pageIndicatorMaskImage;
		public UIColor PageIndicatorTintColor;
		UIImage currentPageIndicatorImage;
		public UIColor CurrentPageIndicatorTintColor;

		public bool HidesForSinglePage;			// hide the the indicator if there is only one page. default is NO
		public bool DefersCurrentPageDisplay;	// if set, clicking to a new page won't update the currently displayed page until -updateCurrentPageDisplay is called. default is NO

		// Private

		int			displayedPage;
		nfloat				measuredIndicatorWidth;
		nfloat				measuredIndicatorHeight;
		CGImage			pageImageMask;

		Dictionary<NSNumber,String> pageNames;
		Dictionary<NSNumber,UIImage> pageImages;
		Dictionary<NSNumber,UIImage> currentPageImages;
		Dictionary<NSNumber,UIImage> pageImageMasks;
		Dictionary<NSNumber,CGImage> cgImageMasks;

		UIPageControl AccessibilityPageControl;

		private const float DEFAULT_INDICATOR_WIDTH=6.0f;
		private const float DEFAULT_INDICATOR_MARGIN=10.0f;
		private const float MIN_HEIGHT=36.0f;

		private enum SMPageControlImageType
		{
			Normal = 1,
			Current,
			Mask
		};

		private void Initialize()
		{
			numberOfPages = 0;
			HidesForSinglePage = false;
			DefersCurrentPageDisplay = false;

			BackgroundColor = UIColor.Clear;

			measuredIndicatorWidth = DEFAULT_INDICATOR_WIDTH;
			measuredIndicatorHeight = DEFAULT_INDICATOR_WIDTH;
			indicatorDiameter = DEFAULT_INDICATOR_WIDTH;
			indicatorMargin = DEFAULT_INDICATOR_MARGIN;
			alignment = SMPageControlAlignment.Center;
			verticalAlignment = SMPageControlVerticalAlignment.Middle;

			IsAccessibilityElement = true;
			AccessibilityTraits = UIAccessibilityTrait.UpdatesFrequently;
			AccessibilityPageControl = new UIPageControl ();

			pageNames = new Dictionary<NSNumber,String> ();
			pageImages = new Dictionary<NSNumber,UIImage> ();
			currentPageImages = new Dictionary<NSNumber,UIImage> ();
			pageImageMasks = new Dictionary<NSNumber,UIImage> ();
			cgImageMasks = new Dictionary<NSNumber,CGImage> ();
		}

		[Export("initWithCoder:")]
		public SMPageControl (NSCoder coder) : base(coder)
		{
			Initialize();
		}

		[Export("initWithFrame:")]
		public SMPageControl (CGRect rect) : base(rect)		
		{
			Initialize();
		}

		public SMPageControl (IntPtr handle) : base(handle)
		{
			Initialize();
		}

		public override void Draw (CGRect rect)
		{
			base.Draw (rect);
			CGContext context = UIGraphics.GetCurrentContext ();
			RenderPages (context, rect);
		}

		private UIImage GetImageFromDictionary(Dictionary<NSNumber,UIImage>dict,NSNumber index)
		{
			UIImage img = null;
			if (dict.TryGetValue (index, out img))
			{
				return img;
			}
			return null;
		}

		private CGImage GetImageFromDictionary(Dictionary<NSNumber,CGImage>dict,NSNumber index)
		{
			CGImage img = null;
			if (dict.TryGetValue (index, out img))
			{
				return img;
			}
			return null;
		}

		private nfloat Floor(nfloat val)
		{
			return (nfloat)Math.Floor (val);
		}

		public void RenderPages(CGContext context,CGRect rect)
		{
			if (numberOfPages < 2 && HidesForSinglePage) 
			{
				return;
			}

			nfloat left = LeftOffset;

			nfloat xOffset = left;
			nfloat yOffset = 0.0f;
			UIColor fillColor = null;
			UIImage image = null;
			CGImage maskingImage = null;
			CGSize maskSize = new CGSize();

			for (uint i = 0; i < numberOfPages; i++) 
			{
				NSNumber indexNumber = new NSNumber(i);

				if (i == displayedPage) 
				{
					fillColor = CurrentPageIndicatorTintColor !=null ? CurrentPageIndicatorTintColor : UIColor.White;
					image = GetImageFromDictionary(currentPageImages,indexNumber);
					if (null == image) 
					{
						image = currentPageIndicatorImage;
					}
				} 
				else 
				{
					fillColor = PageIndicatorTintColor !=null ? PageIndicatorTintColor : UIColor.White.ColorWithAlpha (0.3f);						
					image = GetImageFromDictionary(pageImages,indexNumber);
					if (null == image) 
					{
						image = pageIndicatorImage;
					}
				}

				// If no finished images have been set, try a masking image
				if (null == image) 
				{
					maskingImage = GetImageFromDictionary(cgImageMasks,indexNumber);
					UIImage originalImage = GetImageFromDictionary(pageImageMasks,indexNumber);
					if (originalImage!=null)
						maskSize = originalImage.Size;

					// If no per page mask is set, try for a global page mask!
					if (null == maskingImage) 
					{
						maskingImage = pageImageMask;
						if (pageIndicatorMaskImage!=null)
							maskSize = pageIndicatorMaskImage.Size;
					}
				}

				fillColor.SetFill();

				if (image!=null) 
				{
					yOffset = TopOffsetForHeight(image.Size.Height,rect);
					var centeredXOffset = xOffset + Floor((measuredIndicatorWidth - image.Size.Width) / 2.0f);
					image.Draw(new CGPoint(centeredXOffset, yOffset));
				} 
				else if (maskingImage!=null) 
				{
					yOffset = TopOffsetForHeight(maskSize.Height,rect);
					var centeredXOffset = xOffset + Floor((measuredIndicatorWidth - maskSize.Width) / 2.0f);
					CGRect imageRect = new CGRect(centeredXOffset,yOffset,maskSize.Width,maskSize.Height);
					context.DrawImage(imageRect,maskingImage);
				} 
				else 
				{
					yOffset = TopOffsetForHeight(indicatorDiameter,rect);
					var centeredXOffset = xOffset + Floor((measuredIndicatorWidth - indicatorDiameter) / 2.0f);
					context.FillEllipseInRect(new CGRect(centeredXOffset,yOffset,indicatorDiameter,indicatorDiameter));
				}

				maskingImage = null;
				xOffset += measuredIndicatorWidth + indicatorMargin;
			}

		}

		private nfloat CGRectGetMaxX(CGRect r)
		{
			return r.X + r.Width;
		}

		private nfloat CGRectGetMidX(CGRect r)
		{
			return r.X + (r.Width/2.0f);
		}

		private nfloat CGRectGetMidY(CGRect r)
		{
			return r.Y + (r.Height / 2.0f);
		}

		private nfloat CGRectGetMaxY(CGRect r)
		{
			return r.Y + r.Height;
		}

		private nfloat LeftOffset
		{
			get
			{
				CGRect rect = Bounds;
				CGSize size = SizeForNumberOfPages(numberOfPages);					

				nfloat left = 0.0f;
				switch (alignment) 
				{
					case SMPageControlAlignment.Center: left = CGRectGetMidX(rect) - (size.Width / 2.0f); break;
					case SMPageControlAlignment.Right: left = CGRectGetMaxX(rect) - size.Width; break;
					default: break;
				}

				return left;
			}
		}

		private nfloat TopOffsetForHeight(nfloat height,CGRect rect)
		{
			nfloat top = 0.0f;
			switch (verticalAlignment) 
			{
				case SMPageControlVerticalAlignment.Middle: top = CGRectGetMidY(rect) - (height / 2.0f); break;
				case SMPageControlVerticalAlignment.Bottom: top = CGRectGetMaxY(rect) - height; break; 
				default: break;
			}
			return top;
		}

		public int CurrentPage
		{
			get
			{
				return currentPage;
			}
			set
			{
				SetCurrentPage (value);
			}
		}

		public int Pages
		{
			get
			{
				return numberOfPages;
			}
			set
			{
				SetNumberOfPages (value);
			}
		}

		// update page display to match the currentPage. ignored if defersCurrentPageDisplay is NO. setting the page value directly will update immediately
		public void UpdateCurrentPageDisplay()
		{
			displayedPage = currentPage;
			SetNeedsDisplay();
		}

		public CGRect RectForPageIndicator(int pageIndex)
		{
			if (pageIndex < 0 || pageIndex >= numberOfPages) 
			{
				return new CGRect();
			}

			var left = LeftOffset;
			CGSize size = SizeForNumberOfPages(pageIndex + 1);
			CGRect rect = new CGRect(left + size.Width - measuredIndicatorWidth,0,measuredIndicatorWidth,measuredIndicatorWidth);
			return rect;
		}

		public CGSize SizeForNumberOfPages(int pageCount)
		{
			var marginSpace = Math.Max(0,pageCount-1) * indicatorMargin;
			var indicatorSpace = pageCount * measuredIndicatorWidth;
			CGSize size = new CGSize(marginSpace + indicatorSpace, measuredIndicatorHeight);
			return size;
		}

		private void SetImage(UIImage image,int pageIndex,SMPageControlImageType type)
		{
			if (pageIndex < 0 || pageIndex >= numberOfPages) 
			{
				return;
			}

			Dictionary<NSNumber,UIImage> dictionary = null;
			switch (type) 
			{
				case SMPageControlImageType.Current: dictionary = currentPageImages; break;
				case SMPageControlImageType.Normal: dictionary = pageImages; break;
				case SMPageControlImageType.Mask: dictionary = pageImageMasks; break;
				default: break;
			}

			if (image!=null) 
			{
				dictionary[new NSNumber(pageIndex)] = image;
			} 
			else 
			{
				dictionary.Remove (new NSNumber(pageIndex));
			}
		}

		public void SetImage(UIImage image,int pageIndex)
		{
			SetImage (image, pageIndex, SMPageControlImageType.Normal);
			UpdateMeasuredIndicatorSizes ();
		}

		public void SetCurrentImage(UIImage image,int pageIndex)
		{
			SetImage (image, pageIndex, SMPageControlImageType.Current);
			UpdateMeasuredIndicatorSizes ();
		}

		public void SetImageMask(UIImage image,int pageIndex)
		{
			SetImage (image, pageIndex, SMPageControlImageType.Mask);
			if (null == image)
			{
				cgImageMasks.Remove (new NSNumber(pageIndex));
				return;
			}

			CGImage maskImage = CreateMaskForImage (image);
			if (maskImage != null)
			{
				cgImageMasks[new NSNumber(pageIndex)] = maskImage;
				UpdateMeasuredIndicatorSizeWithSize(image.Size);
				SetNeedsDisplay ();
			}
		}

		private UIImage ImageForPage(int pageIndex,SMPageControlImageType type)
		{
			if (pageIndex < 0 || pageIndex >= numberOfPages) 
			{
				return null;
			}

			Dictionary<NSNumber,UIImage> dictionary = null;
			switch (type) 
			{
				case SMPageControlImageType.Current: dictionary = currentPageImages; break;
				case SMPageControlImageType.Normal: dictionary = pageImages; break;
				case SMPageControlImageType.Mask: dictionary = pageImageMasks; break;
				default: break;
			}

			return GetImageFromDictionary(dictionary,new NSNumber(pageIndex));
		}

		public UIImage ImageForPage(int pageIndex)
		{
			return ImageForPage (pageIndex, SMPageControlImageType.Normal);
		}

		public UIImage CurrentImageForPage(int pageIndex)
		{
			return ImageForPage (pageIndex, SMPageControlImageType.Current);
		}

		public UIImage ImageMaskForPage(int pageIndex)
		{
			return ImageForPage (pageIndex, SMPageControlImageType.Mask);
		}

		public override void SizeToFit()
		{
			CGRect frame = Frame;
			CGSize size = SizeForNumberOfPages(numberOfPages);
			size.Height = (nfloat)Math.Max(size.Height,MIN_HEIGHT);
			frame.Size = size;
			Frame = frame;
		}

		public void UpdatePageNumberForScrollView(UIScrollView scrollView)
		{
			int page = (int)Floor(scrollView.ContentOffset.X / scrollView.Bounds.Width);
			currentPage = page;
		}

		public void SetScrollViewContentOffsetForCurrentPage(UIScrollView scrollView,bool animated)
		{
			CGPoint offset = scrollView.ContentOffset;
			offset.X = scrollView.Bounds.Width * currentPage;
			scrollView.SetContentOffset (offset, animated);
		}

		private CGImage CreateMaskForImage(UIImage image)
		{
			int pixelsWide = (int)(image.Size.Width * image.CurrentScale);
			int pixelsHigh = (int)(image.Size.Height * image.CurrentScale);
			int bitmapBytesPerRow = (pixelsWide * 1);
			CGBitmapContext context = new CGBitmapContext(null,pixelsWide,pixelsHigh,image.CGImage.BitsPerComponent,bitmapBytesPerRow,null,CGImageAlphaInfo.Only);
			context.TranslateCTM(0.0f, pixelsHigh);
			context.ScaleCTM(1.0f, -1.0f);
			context.DrawImage(new CGRect(0, 0, pixelsWide, pixelsHigh),image.CGImage);
			CGImage maskImage = context.ToImage ();
			//CGImage maskImage = CGBitmapContextCreateImage(context);
			//CGContextRelease(context);
			return maskImage;
		}

		private void UpdateMeasuredIndicatorSizeWithSize(CGSize size)
		{
			measuredIndicatorWidth = (nfloat)Math.Max(measuredIndicatorWidth, size.Width);
			measuredIndicatorHeight = (nfloat)Math.Max(measuredIndicatorHeight, size.Height);
		}

		private void UpdateMeasuredIndicatorSizes()
		{
			measuredIndicatorWidth = indicatorDiameter;
			measuredIndicatorHeight = indicatorDiameter;

			// If we're only using images, ignore the _indicatorDiameter
			if ( (pageIndicatorImage!=null || pageIndicatorMaskImage!=null) && currentPageIndicatorImage!=null )
			{
				measuredIndicatorWidth = 0;
				measuredIndicatorHeight = 0;
			}

			if (pageIndicatorImage!=null) 
			{
				UpdateMeasuredIndicatorSizeWithSize (pageIndicatorImage.Size);
			}

			if (currentPageIndicatorImage!=null) 
			{
				UpdateMeasuredIndicatorSizeWithSize(currentPageIndicatorImage.Size);
			}

			if (pageIndicatorMaskImage!=null) 
			{
				UpdateMeasuredIndicatorSizeWithSize(pageIndicatorMaskImage.Size);
			}
		}

		#region Tap Gesture

		// We're using touchesEnded: because we want to mimick UIPageControl as close as possible
		// As of iOS 6, UIPageControl still (as far as we know) does not use a tap gesture recognizer. This means that actions like
		// touching down, sliding around, and releasing, still results in the page incrementing or decrementing.
		public override void TouchesEnded(NSSet touches,UIEvent anEvent)
		{
			UITouch touch = touches.AnyObject as UITouch;
			CGPoint point = touch.LocationInView(this);
			CGSize size = SizeForNumberOfPages(numberOfPages);
			var left = LeftOffset;
			var middle = left + (size.Width / 2.0f);
			if (point.X < middle) 
			{
				SetCurrentPage (currentPage-1, sendEvent: true, canDefer: true);
			} 
			else 
			{
				SetCurrentPage (currentPage+1, sendEvent: true, canDefer: true);
			}
		}

		#endregion

		public override CGRect Frame
		{
			set
			{
				base.Frame = value;
				SetNeedsDisplay ();
			}
			get
			{
				return base.Frame;
			}
		}

		public void SetIndicatorDiameter(float _indicatorDiameter)
		{
			if (indicatorDiameter == _indicatorDiameter) 
			{
				return;
			}

			indicatorDiameter = _indicatorDiameter;
			UpdateMeasuredIndicatorSizes ();
			SetNeedsDisplay ();
		}

		public void SetIndicatorMargin(float _indicatorMargin)
		{
			if (indicatorMargin == _indicatorMargin) 
			{
				return;
			}

			indicatorMargin = _indicatorMargin;
			SetNeedsDisplay ();
		}

		public void SetNumberOfPages(int _numberOfPages)
		{
			if (numberOfPages == _numberOfPages) 
			{
				return;
			}


			AccessibilityPageControl.Pages = numberOfPages;

			numberOfPages = Math.Max(0,_numberOfPages);
			UpdateAccessibilityValue ();
			SetNeedsDisplay ();
		}

		public void SetCurrentPage(int _currentPage)
		{
			SetCurrentPage(_currentPage,sendEvent:false,canDefer:false);
		}

		public void SetCurrentPage(int _currentPage,bool sendEvent,bool canDefer)
		{	
			currentPage = Math.Min(Math.Max(0, _currentPage),numberOfPages - 1);
			AccessibilityPageControl.CurrentPage = currentPage;

			UpdateAccessibilityValue ();

			if (false == DefersCurrentPageDisplay || false == canDefer) 
			{
				displayedPage = currentPage;
				SetNeedsDisplay ();
			}

			if (sendEvent) 
			{
				SendActionForControlEvents (UIControlEvent.ValueChanged);
			}
		}

		public void SetCurrentPageIndicatorImage(UIImage _currentPageIndicatorImage)
		{
			if (currentPageIndicatorImage!=null && currentPageIndicatorImage.Equals(_currentPageIndicatorImage)) 
			{
				return;
			}

			currentPageIndicatorImage = _currentPageIndicatorImage;
			UpdateMeasuredIndicatorSizes ();
			SetNeedsDisplay ();
		}

		public void SetPageIndicatorImage(UIImage _pageIndicatorImage)
		{
			if (pageIndicatorImage!=null && pageIndicatorImage.Equals(_pageIndicatorImage)) 
			{
				return;
			}

			pageIndicatorImage = _pageIndicatorImage;
			UpdateMeasuredIndicatorSizes ();
			SetNeedsDisplay ();
		}

		public void SetPageIndicatorMaskImage(UIImage _pageIndicatorMaskImage)
		{
			if (pageIndicatorMaskImage!=null && pageIndicatorMaskImage.Equals(pageIndicatorMaskImage)) 
			{
				return;
			}

			pageIndicatorMaskImage = _pageIndicatorMaskImage;

			/*
			if (pageImageMask) 
			{
				CGImageRelease(pageImageMask);
			}*/

			pageImageMask = CreateMaskForImage (pageIndicatorMaskImage);

			UpdateMeasuredIndicatorSizes ();
			SetNeedsDisplay ();
		}


		#region UIAccessibility

		public void SetName(String name,int pageIndex)
		{
			if (pageIndex < 0 || pageIndex >= numberOfPages) 
			{
				return;
			}

			pageNames[new NSNumber(pageIndex)] = name;
		}

		public String NameForPage(int pageIndex)
		{
			if (pageIndex < 0 || pageIndex >= numberOfPages) 
			{
				return null;
			}

			String val = null;
			if (pageNames.TryGetValue (new NSNumber(pageIndex), out val))
				return val;
			return null;
		}

		public void UpdateAccessibilityValue()
		{
			String pageName = NameForPage(currentPage);
			String accessibilityValue = AccessibilityPageControl.AccessibilityValue;

			if (pageName!=null) 
			{
				AccessibilityValue = String.Format("{0} - {1}",pageName, accessibilityValue);					
			} 
			else 
			{
				if (accessibilityValue == null)
					AccessibilityValue = "";
				else
					AccessibilityValue = accessibilityValue;
			}
		}

		#endregion
	}
}
