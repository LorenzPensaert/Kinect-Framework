using HelixToolkit.Wpf;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace KinectSDK.Logic {

    /// <summary>
    /// Kinect Draw to draw/render KinectObjects/Skeleton
    /// </summary>
    public static class KinectDraw {

        /// <summary>
        /// Draw the Kinect Objects onto a Body within a canvas
        /// </summary>
        /// <param name="canvas">The Canvas to paint images on</param>
        /// <param name="sensor">The Kinect sensor</param>
        /// <param name="body">The body to locate the images on</param>
        /// <param name="kinectObjects">The Kinect Object</param>
        public static void DrawKinectObjects (this Canvas canvas, KinectSensor sensor, Body body, List<KinectObject> kinectObjects) {
            if (kinectObjects == null) return;
            
            foreach (KinectObject kinectObject in kinectObjects) {
                Joint firstJoint = body.Joints[(JointType)kinectObject.FirstJoint],
                      secondJoint = body.Joints[(JointType) kinectObject.SecondJoint];

                
                if (firstJoint.TrackingState == TrackingState.Tracked  && secondJoint.TrackingState == TrackingState.Tracked) {
                    CameraSpacePoint firstJointPos = firstJoint.Position,
                                     secondJointPos = secondJoint.Position;

                    Point firstPoint = new Point(), 
                          secondPoint = new Point();

                    ColorSpacePoint firstColorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(firstJointPos),
                                    secondColorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(secondJointPos);

                    firstPoint.X = float.IsInfinity(firstColorPoint.X) ? 0 : firstColorPoint.X;
                    firstPoint.Y = float.IsInfinity(firstColorPoint.Y) ? 0 : firstColorPoint.Y;

                    secondPoint.X = float.IsInfinity(secondColorPoint.X) ? 0 : secondColorPoint.X;
                    secondPoint.Y = float.IsInfinity(secondColorPoint.Y) ? 0 : secondColorPoint.Y;
                        
                    firstJoint = firstJoint.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);
                    secondJoint = secondJoint.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);

                    Image bmp = new Image()
                    {
                        Width = KinectUtils.DistanceBetween(firstJoint, secondJoint) * 1.5,
                        Height = KinectUtils.DistanceBetween(firstJoint, secondJoint) * 1.5
                    };

                    Canvas.SetTop(bmp, (firstPoint.Y + (secondPoint.Y - firstPoint.Y) / 2 - kinectObject.OffsetY) - bmp.Height / 2);
                    Canvas.SetLeft(bmp, (firstPoint.X + (secondPoint.X - firstPoint.X) / 2 + kinectObject.OffsetX) - bmp.Width / 2);

                    bmp.RenderTransformOrigin = new Point(0.5f, 0.5f);

                    var rotateT = new RotateTransform(KinectUtils.AngleBetween(firstJoint, secondJoint) + 90);
                    var skewT = new SkewTransform(KinectUtils.YAngleBetween(firstJoint, secondJoint), KinectUtils.YAngleBetween(firstJoint, secondJoint));

                    var transformGroup = new TransformGroup();
                    transformGroup.Children.Add(rotateT);
                    transformGroup.Children.Add(skewT);
                    bmp.RenderTransform = transformGroup;

                    //Load the image
                    bmp.Source = kinectObject.Image;
                    canvas.Children.Add(bmp);
                }
            }
        }

        /// <summary>
        /// Render the Kinect 3D Objects onto a Body within a 3D View
        /// </summary>
        /// <param name="view">The 3D View</param>
        /// <param name="kinectObjects">The Kinect Object to render</param>
        public static void RenderKinect3DObjects (this ModelVisual3D view, List<Kinect3DObject> kinectObjects) {
            if (kinectObjects == null) return;
            var modelGroup = new Model3DGroup();
            foreach (Kinect3DObject kinectObject in kinectObjects) {
                var grpTransfo = new Transform3DGroup();
                grpTransfo.Children.Add(new TranslateTransform3D());
                grpTransfo.Children.Add(new RotateTransform3D());
                grpTransfo.Children.Add(new ScaleTransform3D());
                modelGroup.Children.Add(kinectObject.Model);
            }
            view.Content = modelGroup;
        }

        /// <summary>
        /// Update the Kinect Objects to the current Body location/rotation
        /// </summary>
        /// <param name="body">The Body</param>
        /// <param name="kinectObjects">The Kinect 3D Objects to update</param>
        public static void UpdateKinect3DObjects (Body body, List<Kinect3DObject> kinectObjects) {
            if (kinectObjects == null) return;
            foreach (Kinect3DObject kinectObject in kinectObjects) {
                Joint j1 = body.Joints[(JointType) kinectObject.FirstJoint];
                JointOrientation jo1 = body.JointOrientations[(JointType) kinectObject.FirstJoint];
                var grpTransfo = new Transform3DGroup();

                if (kinectObject.IsMoveable) {
                    kinectObject.OffsetX = (j1.Position.X * 10);
                    kinectObject.OffsetY = (j1.Position.Y * 10);
                    grpTransfo.Children.Add(new TranslateTransform3D(-kinectObject.OffsetX, kinectObject.OffsetY, 0));
                }

                if (kinectObject.IsRotateable) {
                    grpTransfo.Children.Add(new RotateTransform3D(
                        new QuaternionRotation3D(
                            new Quaternion(jo1.Orientation.X, -jo1.Orientation.Y, -jo1.Orientation.Z, jo1.Orientation.W)),
                            j1.Position.X,
                            j1.Position.Y,
                            j1.Position.Z));
                }

                kinectObject.Model.Transform = grpTransfo;
            }
        }

        /// <summary>
        /// Draws dots on the visible bodyparts
        /// </summary>
        /// <param name="canvas">The canvas to draw on</param>
        /// <param name="sensor">The kinect sensor</param>
        /// <param name="body">the body</param>
        public static void DrawDots (this Canvas canvas, KinectSensor sensor, Body body) {
            if (body == null) return;

            foreach (Joint joint in body.Joints.Values)
            {
                if (joint.TrackingState == TrackingState.Tracked)
                {
                    // 3D space point
                    CameraSpacePoint jointPosition = joint.Position;

                    // 2D space point
                    Point point = new Point();

                    ColorSpacePoint colorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(jointPosition);

                    point.X = float.IsInfinity(colorPoint.X) ? 0 : colorPoint.X;
                    point.Y = float.IsInfinity(colorPoint.Y) ? 0 : colorPoint.Y;

                    // Draw
                    Ellipse ellipse = new Ellipse
                    {
                        Fill = Brushes.White,
                        Width = 15,
                        Height = 15
                    };

                    Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                    Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

                    canvas.Children.Add(ellipse);
                }
            }
        }
    }
}