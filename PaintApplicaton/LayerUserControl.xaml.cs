using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApplication
{
    /// <summary>
    /// Interaction logic for LayerUserControl.xaml
    /// </summary>
    public partial class LayerUserControl : UserControl
    {
        private readonly Path overlayPath;
        private readonly PathFigure pathFigure;
        private readonly PathGeometry pathGeometry;
        private StreamGeometryContext ctx;
        private StreamGeometry geometry;
        private bool isDragging;
        private bool isMouseDown;
        private bool isSketchMode;
        private Point panStartPosition;
        private Stack<UIElement> redoElements;

        public LayerUserControl()
        {
            InitializeComponent();
            pathFigure = new PathFigure();
            pathGeometry = new PathGeometry();
            overlayPath = new Path
            {
                Stroke = Brushes.Red
            };

            isSketchMode = true;

            AnnotationTextBox.KeyUp += AnnotationEnterKeyDown;

            redoElements = new Stack<UIElement>();
        }

        private void AnnotationEnterKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddAnnotationElement();
            }
        }

        private void AddAnnotationElement()
        {
            if (!string.IsNullOrEmpty(AnnotationTextBox.Text))
            {
                var textBlock = new TextBlock
                {
                    Text = AnnotationTextBox.Text,
                    FontSize = 22,
                    FontStretch = FontStretches.UltraExpanded,
                    FontWeight = FontWeights.Normal,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Background = Brushes.Transparent,
                    Foreground = Brushes.Green
                };

                Canvas.SetLeft(textBlock, panStartPosition.X);
                Canvas.SetTop(textBlock, panStartPosition.Y);
                AnnotationLayer.Children.Add(textBlock);
                AnnotationControl.Visibility = Visibility.Hidden;
                AnnotationTextBox.Text = "";
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                CaptureMouse();
                isMouseDown = true;
                if (isSketchMode)
                {
                    isDragging = false;

                    ClearOverlayRelative();

                    overlayPath.StrokeThickness = 10;
                }
                else
                {
                    ClearOverlayRelative();

                    panStartPosition = e.GetPosition(this);
                    Canvas.SetLeft(AnnotationControl, panStartPosition.X);
                    Canvas.SetTop(AnnotationControl, panStartPosition.Y);
                    AnnotationControl.Visibility = Visibility.Visible;
                    AnnotationTextBox.Focus();
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isMouseDown)
            {
                if (isSketchMode)
                {
                    if (!isDragging)
                    {
                        pathFigure.StartPoint = e.GetPosition(TopAnnotationLayer);
                        pathGeometry.Figures.Add(pathFigure);

                        overlayPath.Data = pathGeometry;
                        TopAnnotationLayer.Children.Add(overlayPath);

                        geometry = new StreamGeometry();
                        ctx = geometry.Open();
                        ctx.BeginFigure(
                            new Point(e.GetPosition(AnnotationLayer).X, e.GetPosition(AnnotationLayer).Y),
                            true,
                            false);
                    }

                    ctx.LineTo(
                        new Point(e.GetPosition(AnnotationLayer).X, e.GetPosition(AnnotationLayer).Y),
                        true,
                        true);

                    pathFigure.Segments.Add(
                        new LineSegment(e.GetPosition(TopAnnotationLayer), true)
                    );
                }

                isDragging = true;
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                ReleaseMouseCapture();

                if (isSketchMode)
                {
                    ClearOverlayRelative();

                    if (isDragging)
                    {
                        var sketchPath = new Path
                        {
                            Stroke = Brushes.GreenYellow,
                            StrokeThickness = 1.5,
                            Data = geometry
                        };
                        AnnotationLayer.Children.Add(sketchPath);
                        ((IDisposable)ctx).Dispose();
                    }
                }

                isMouseDown = false;
                isDragging = false;
            }
        }

        private void ClearOverlayRelative()
        {
            pathFigure.Segments.Clear();
            pathGeometry.Clear();
            TopAnnotationLayer.Children.Clear();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            AnnotationControl.Visibility = Visibility.Hidden;
            AnnotationTextBox.Text = "";
        }

        private void ApplyButton_OnClick(object sender, RoutedEventArgs e)
        {
            AddAnnotationElement();
        }

        private void SketchButton_OnClick(object sender, RoutedEventArgs e)
        {
            isSketchMode = true;
        }

        private void NotationButton_OnClick(object sender, RoutedEventArgs e)
        {
            isSketchMode = false;
        }

        private void UndoButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (AnnotationLayer.Children.Count > 0)
            {
                var childrenIndex = AnnotationLayer.Children.Count - 1;
                redoElements.Push(AnnotationLayer.Children[childrenIndex]);
                AnnotationLayer.Children.RemoveAt(childrenIndex);
            }
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            AnnotationLayer.Children.Clear();
            redoElements.Clear();
        }

        private void RedoButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (redoElements.Count > 0)
            {
                AnnotationLayer.Children.Add(redoElements.Pop());
            }
        }
    }
}