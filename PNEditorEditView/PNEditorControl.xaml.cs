using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Markup;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Linq;
using Core;
using PNEditorEditView.ModelArrange;
using PNEditorEditView.PropertyControls;
using PNEditorEditView.Util;
using Path = System.Windows.Shapes.Path;
using Core.Model;
using PNEditorEditView.Model;

namespace PNEditorEditView
{
    /// <summary>
    /// Author: Natalia Nikitina / Alexey Mitsyuk
    /// Carassius GUI
    /// PAIS Lab, 2014 - 2015
    /// </summary>
    public partial class PNEditorControl : IView
    {
        private const string ADDARC = "Add new arc";
        private const string ADDNODE = "Add new node";
        private const string ADDEDGE = "Add new edge";

        private const string ARCSBETWEENTRANSITIONS = "Arcs between transitions are not allowed!";
        private const string ARCSBETWEENPLACES = "Arcs between places are not allowed!";
        private const string ARCALREADYEXISTS = "An arc is already exists";

        private const string INCORRECTDATA = "The number of tokens should be bigger than 0.\n Please, try again!";
        private const string INCORRECTDATA01 = "You have entered the incorrect text data.\n Please, try again!";

        private const string INCORRECTDATA02 =
            "The number of tokens should be bigger than or equal to 0.\n Please, try again!";

        private const string INCORRECTDATA03 = "An arc's weight should be bigger than 0.\n Please, try again!";

        private const string CLEARCANVAS00 = "Are you sure, that you want to clear the model?";
        private const string CLEARCANVAS01 = "Clear ...";

        private const string EMPTYMODEL = "Current model is empty!";

        private const string ALWAYSTIENODES00 = "Do not tie nodes to the grid";
        private const string ALWAYSTIENODES01 = "Always tie nodes to the grid";

        private const string NODECANTBEINITIALANDFINAL = "Node can't be initial and final at the same time";

        private const int PETRINETCELLHEIGHT = 70;
        private const int PETRINETCELLWIDTH = 60;
        private const int PLACEWIDTH = 30;
        private const int PLACEHEIGHT = 30;
        private const int TRANSITIONWIDTH = 20;
        private const int TRANSITIONHEIGHT = 50;

        private const double EPSILON = 0.00001;
        private const double DISTANCE = 80;

        // main Petri-net
        public static VPetriNet Net = VPetriNet.Create();

        //todo Сейчас один выравниватель. В будущем предполагается заменить на набор разных.
        private readonly IArranger _arranger;

        public PNEditorControl()
        {
            InitializeComponent();
            InitProperties();
            // this line is needed to draw the line during Line Drawing
            MainModelCanvas.Children.Add(_lineArcDrawing);
            // this is Select Rectangle
            AddSelectRectangeOnCanvas(_selectRect);


            Stopwatch.Start();

            _arranger = new PetriNetColumnAndGraphForceBasedGeneralArranger();
        }

        private List<PropertyEditorBase> _propertyEditors;

        private void InitProperties()
        {
            _propertyEditors = new List<PropertyEditorBase>();
            foreach (var child in PropertiesPanel.Children)
            {
                var editor = child as PropertyEditorBase;
                if (editor != null)
                {
                    _propertyEditors.Add(editor);
                    editor.BindMainControl(this);
                    editor.SetItem(null);
                }
            }
        }

        #region Grid-Drawing

        private readonly List<Rectangle> _gridDots = new List<Rectangle>();

        private void AddGridOnCanvas()
        {
            double x1 = 0.5, y1 = -2;
            while (y1 < MainModelCanvas.ActualHeight)
            {
                while (x1 < MainModelCanvas.ActualWidth)
                {
                    var dot = new Rectangle
                    {
                        RenderTransform = new RotateTransform(45),
                        Height = 3,
                        Width = 3,
                        Visibility = Visibility.Visible,
                        Stroke = Brushes.Gray
                    };
                    Panel.SetZIndex(dot, -1);
                    _gridDots.Add(dot);
                    Canvas.SetTop(dot, y1);
                    Canvas.SetLeft(dot, x1);
                    MainModelCanvas.Children.Add(dot);
                    x1 += 60;
                }

                y1 += 70;
                x1 = 0.5;
            }
        }

        private void RemoveGridFromCanvas()
        {
            foreach (var rectangle in _gridDots)
            {
                MainModelCanvas.Children.Remove(rectangle);
            }
        }

        private bool showGrid;

        private void HideOrShowGrid(double scale)
        {
            if (scale < 1 || showGrid)
            {
                foreach (Rectangle rectangle in _gridDots)
                    rectangle.Visibility = Visibility.Hidden;
            }
            else
            {
                foreach (Rectangle rectangle in _gridDots)
                    rectangle.Visibility = Visibility.Visible;
            }

            btnGrid.IsEnabled = !(scale < 1);
        }

        #endregion Grid-Drawing

        #region UIMethods

        private void EnableAddButtons()
        {
            btnAddPlace.IsEnabled = true;
            btnAddTransition.IsEnabled = true;
            btnAddArc.IsEnabled = true;
            btnAddToken.IsEnabled = true;
            btnNonOrientedArc.IsEnabled = true;
            btnSetInitialState.IsEnabled = true;
            btnInsertHandle.IsEnabled = true;
        }

        public void DisableRedoButton()
        {
            if (Command.CanceledCommands.Count == 0)
                btnRedo.IsEnabled = false;
        }

        private void EnableUndoRedoButtons()
        {
            btnUndo.IsEnabled = true;
            btnRedo.IsEnabled = true;
        }

        #endregion UIMethods

        #region ClickHandlers

        private void btnAddPlace_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;
            _leftMouseButtonMode = LeftMouseButtonMode.AddPlace;
            EnableAddButtons();
            btnAddPlace.IsEnabled = false;
        }

        private void btnAddTransition_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;
            _leftMouseButtonMode = LeftMouseButtonMode.AddTransition;
            EnableAddButtons();
            btnAddTransition.IsEnabled = false;
        }

        private void btnAddArc_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;
            _leftMouseButtonMode = LeftMouseButtonMode.AddArc;
            EnableAddButtons();
            btnAddArc.IsEnabled = false;
        }

        private void btnAddToken_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;
            _leftMouseButtonMode = LeftMouseButtonMode.AddToken;
            EnableAddButtons();
            btnAddToken.IsEnabled = false;
        }

        private void btnInsertHandle_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;
            _leftMouseButtonMode = LeftMouseButtonMode.InsertHandle;
            EnableAddButtons();
            btnInsertHandle.IsEnabled = false;
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = false;
            _leftMouseButtonMode = LeftMouseButtonMode.Select;
            EnableAddButtons();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;
            MakeDeleteCommand(_selectedFigures, _selectedArcs);
            DeleteFigures(_selectedFigures, _selectedArcs);
            _selectedFigures.Clear();
            _selectedArcs.Clear();
            ReassignSelectedProperties();
            btnUndo.IsEnabled = true;
        }

        private void btnGrid_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;
            if (showGrid == false)
            {
                showGrid = true;
                foreach (var rectangle in _gridDots)
                    rectangle.Visibility = Visibility.Hidden;
            }
            else
            {
                showGrid = false;
                foreach (var rectangle in _gridDots)
                    rectangle.Visibility = Visibility.Visible;
            }

            TurnOnSelectMode();
        }

        private void btnNonOrientedArc_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;
            _leftMouseButtonMode = LeftMouseButtonMode.AddArc;
            EnableAddButtons();
            btnNonOrientedArc.IsEnabled = false;
        }

        private void btnTieToMeshAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (PetriNetNode figure in Net.Nodes)
            {
                if (!_selectedFigures.Contains(figure))
                {
                    _selectedFigures.Add(figure);
                }
            }

            btnTieToMesh_Click(sender, e);
            UnselectFigures(); //(selectedFigures, selectedArcs); //(PetriNetNode.figures, Arc.arcs);
            ReassignSelectedProperties();
            EnableAddButtons();
        }

        private void btnTieToMeshAlways_Click(object sender, RoutedEventArgs e)
        {
            if (_tie == false)
            {
                btnTieToMeshAlways.ToolTip = ALWAYSTIENODES00;
                _modeTieToMesh = ModeTieToMesh.Tie;
                _tie = true;
            }
            else
            {
                btnTieToMeshAlways.ToolTip = ALWAYSTIENODES01;
                _modeTieToMesh = ModeTieToMesh.NotTie;
                _tie = false;
            }
        }

        [MenuItemHandler("file/export/as LaTeX code",27)]
        public void MenuMakeTeX_Click()
        {
            btnSelect.IsEnabled = true;
            if (Net.Nodes.Count == 0)
            {
                MessageBox.Show(EMPTYMODEL);
                return;
            }

            PNtoTeXSettings PetriTeXWindow = new PNtoTeXSettings();
            PetriTeXWindow.ShowDialog();
        }
        [MenuItemHandler("help/about")]
        public static void MenuAbout_Click()
        {
            //btnSelect.IsEnabled = true;
            PNEditorAbout aboutWin = new PNEditorAbout();
            aboutWin.ShowDialog();
        }
        [MenuItemHandler("file/exit",100)]
        public static void MenuExit_Click()
        {
            Application.Current.Shutdown();
        }

        private void btnShowHideLabels_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;
            if (hideLabels == false)
            {
                foreach (var label in NodesToLabelsInCanvas.Values)
                    label.Visibility = Visibility.Hidden;

                hideLabels = true;
            }
            else
            {
                foreach (var label in NodesToLabelsInCanvas.Values)
                    label.Visibility = Visibility.Visible;

                hideLabels = false;
            }

            TurnOnSelectMode();
        }

        private void ChangePriorities(List<PetriNetNode> selectedF, List<VArc> selectedA, int change)
        {
            _leftMouseButtonMode = change < 0 ? LeftMouseButtonMode.PriorityDown : LeftMouseButtonMode.PriorityUp;
            EnableAddButtons();

            if (selectedF.Count != 0)
            {
                foreach (PetriNetNode figure in selectedF)
                {
                    //RemoveNode(figure);
                    VTransition t = figure as VTransition;
                    if (t != null)
                    {
                        t.Priority += change;
                        // TODO: add priority label
                    }
                }
            }

            //DeleteArcs(selectedA);

            ReassignSelectedProperties();

            _selectedFigure = null;
            _selectedArc = null;
            TurnOnSelectMode();
        }

        private void MakeChangePriorityCommand(List<PetriNetNode> selectedF, List<VArc> selectedA, int change)
        {
            List<PetriNetNode> deletedFigures = new List<PetriNetNode>();
            List<VArc> deletedArcs = new List<VArc>();
            foreach (PetriNetNode figure in selectedF)
            {
                deletedFigures.Add(figure);
                foreach (VArc arc in figure.ThisArcs)
                    if (!deletedArcs.Contains(arc))
                        deletedArcs.Add(arc);
            }

            foreach (VArc arc in selectedA)
            {
                if (!deletedArcs.Contains(arc))
                    deletedArcs.Add(arc);
            }

            DeleteCommand newCommand = new DeleteCommand(deletedFigures, deletedArcs);
            Command.ExecutedCommands.Push(newCommand);
            Command.CanceledCommands.Clear();
        }

        private void btnZoomMinus_Click(object sender, RoutedEventArgs e)
        {
            if (_scaleTransform.ScaleX > 0.05)
            {
                btnSelect.IsEnabled = true;
                //if (btnEditMode.IsVisible == false)
                //    EnableAddButtons();
                _scaleTransform.ScaleX /= ScaleRate;
                _scaleTransform.ScaleY /= ScaleRate;
                _thisScale /= ScaleRate;
                MainModelCanvas.LayoutTransform = _scaleTransform;
                MainModelCanvas.UpdateLayout();
                VisUtil.ResizeCanvas(Net.Nodes, MainControl, MainModelCanvas);
                HideOrShowGrid(_thisScale);
                TurnOnSelectMode();
                btnZoomPlus.IsEnabled = true;
            }
            else
            {
                btnZoomMinus.IsEnabled = false;
            }
        }

        private void btnZoomPlus_Click(object sender, RoutedEventArgs e)
        {
            if (_scaleTransform.ScaleX < 17.5)
            {
                btnSelect.IsEnabled = true;
                //if (btnEditMode.IsVisible == false)
                //    EnableAddButtons();
                _scaleTransform.ScaleX *= ScaleRate;
                _scaleTransform.ScaleY *= ScaleRate;
                _thisScale *= ScaleRate;
                MainModelCanvas.LayoutTransform = _scaleTransform;
                MainModelCanvas.UpdateLayout();
                VisUtil.ResizeCanvas(Net.Nodes, MainControl, MainModelCanvas);
                HideOrShowGrid(_thisScale);
                TurnOnSelectMode();
                btnZoomMinus.IsEnabled = true;
            }
            else
            {
                btnZoomPlus.IsEnabled = false;
            }
        }

        public void ChangeNumberOfTokensWithCommand(VPlace place, int num)
        {
            //btnSelect.IsEnabled = true;
            //if (_isNumberOfTokensInt == false)
            //{
            //    MessageBox.Show(INCORRECTDATA01);
            //    tbTokenNumber.Text = "";
            //    tbTokenNumber.Focus();
            //}
            //else if (_numberOfTokensChanged < 0)
            //{
            //    MessageBox.Show(INCORRECTDATA);
            //    tbTokenNumber.Text = "";
            //    tbTokenNumber.Focus();
            //}
            //else
            //{
            if (place.NumberOfTokens != 0)
            {
                if (place.NumberOfTokens < 5)
                    RemoveTokens(place);
                else
                    MainModelCanvas.Children.Remove(place.NumberOfTokensLabel);
            }

            AddTokensCommand newCommand = new AddTokensCommand(place, place.NumberOfTokens,
                num);
            Command.ExecutedCommands.Push(newCommand);
            Command.CanceledCommands.Clear();
            place.NumberOfTokens = num;

            if (place.NumberOfTokens == 0)
                RemoveTokens(place);
            if (place.NumberOfTokens >= 0 && place.NumberOfTokens < 5)
                MainModelCanvas.Children.Remove(place.NumberOfTokensLabel);
            AddTokens(place);

            TurnOnSelectMode();
            //}
        }

        public void ChangePriorityWithCommand(VTransition transition, int priority)
        {
            //btnSelect.IsEnabled = true;
            //if (_isPriorityInt == false)
            //{
            //    MessageBox.Show(INCORRECTDATA01);
            //    tbPriority.Text = "";
            //    tbPriority.Focus();
            //}
            //else if (_priorityChanged < 0)
            //{
            //    MessageBox.Show(INCORRECTDATA);
            //    tbPriority.Text = "";
            //    tbPriority.Focus();
            //}
            //else
            //{
            List<VTransition> list = new List<VTransition>();
            list.Add(transition);
            ChangePriorityCommand newCommand = new ChangePriorityCommand(list, priority);
            Command.ExecutedCommands.Push(newCommand);
            Command.CanceledCommands.Clear();
            transition.Priority = priority;

            TurnOnSelectMode();
            //}
        }

        private void btnTieToMesh_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;
            _figuresBeforeDrag = CopyListOfFigures(Net.Nodes);
            foreach (PetriNetNode selected in _selectedFigures)
                SetCoordinatesByMesh(selected);
            _figuresAfterDrag = CopyListOfFigures(Net.Nodes);
            DragCommand newCommand = new DragCommand(_figuresBeforeDrag, _figuresAfterDrag);
            Command.ExecutedCommands.Push(newCommand);
            Command.CanceledCommands.Clear();
            TurnOnSelectMode();
        }

        public void ChangeWeightWithCommand(VArc arc, int weight)
        {
            //btnSelect.IsEnabled = true;
            //if (_isPriorityInt == false)
            //{
            //    MessageBox.Show(INCORRECTDATA01);
            //    tbPriority.Text = "";
            //    tbPriority.Focus();
            //}
            //else if (_priorityChanged < 0)
            //{
            //    MessageBox.Show(INCORRECTDATA);
            //    tbPriority.Text = "";
            //    tbPriority.Focus();
            //}
            //else
            //{

            ChangeWeightCommand newCommand = new ChangeWeightCommand(arc, arc.Weight.ToString(), weight.ToString(),
                arc.WeightLabel, arc.WeightLabel);
            Command.ExecutedCommands.Push(newCommand);
            Command.CanceledCommands.Clear();
            arc.Weight = weight.ToString();

            TurnOnSelectMode();
            //}
        }

        private void btnClearWorkingField_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;
            EnableAddButtons();

            if (MessageBox.Show(CLEARCANVAS00, CLEARCANVAS01,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                List<PetriNetNode> figures = Net.Nodes.ToList();
                List<VArc> arcs = Net.arcs.ToList();
                MakeDeleteCommand(figures, arcs);
                DeleteFigures(figures, arcs);
                btnUndo.IsEnabled = true;
                HideAllProperties();
            }

            TurnOnSelectMode();
        }

        private void btnSaveToPng_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;
            ImportExport.BitmapExporter.SaveModelAsAPicture(MainModelCanvas, Net.Nodes);

            TurnOnSelectMode();
            EnableAddButtons();
        }


        private void btnArrangeModel_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;

            _figuresBeforeDrag = CopyListOfFigures(Net.Nodes);

            _arranger.ArrangePetriNetModel(Net.Nodes, Net.arcs, this);

            _figuresAfterDrag = CopyListOfFigures(Net.Nodes);

            Command.ExecutedCommands.Push(new DragCommand(_figuresBeforeDrag, _figuresAfterDrag));
            Command.CanceledCommands.Clear();

            btnUndo.IsEnabled = true;

            VisUtil.ResizeCanvas(Net.Nodes, MainControl, MainModelCanvas);

            TurnOnSelectMode();
            EnableAddButtons();
            hideLabels = false;
        }

        public void ChangeLabelWithCommand(PetriNetNode node, string label)
        {
            ChangeNameCommand newCommand = new ChangeNameCommand(node, node.Label, label);
            Command.ExecutedCommands.Push(newCommand);
            Command.CanceledCommands.Clear();

            ChangeLabel(node, label);

            btnShowHideLabels.IsEnabled = true;
            TurnOnSelectMode();
        }

        private void TabWithMainModelCanvas_Click(object sender, RoutedEventArgs e)
        {
            pnlLeftToolPanel.Visibility = Visibility.Visible;
        }

        private void btnSetInitialState_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;
            _leftMouseButtonMode = LeftMouseButtonMode.SetInitialState;
            EnableAddButtons();
            btnSetInitialState.IsEnabled = false;
        }

        #endregion ClickHandlers


        #region PetriNetImportExport

        //public void GetPetriNet(PetriNet net)
        //{
        //    Net = PetriNet.Create(net.places, net.transitions, net.arcs);
        //    //foreach (Place place in net.places)
        //    //    SetOfFigures.Figures.Add(place);
        //    //foreach (Transition transition in net.transitions)
        //    //    SetOfFigures.Figures.Add(transition);
        //    //todo А обновится ли вообще картинка?
        //}

        //public PetriNet GivePetriNet()
        //{
        //    return Net.Copy();
        //}

        public VPetriNet GetCurrentModel()
        {
            return Net;
        }

        public void SetCurrentModel(VPetriNet model)
        {
            Net = model;
            foreach (PetriNetNode figure in Net.Nodes)
            {
                DrawFigure(figure);
            }

            foreach (VArc arc in Net.arcs)
            {
                DisplayArc(arc);
            }
        }

        #endregion PetriNetImportExport

        #region Events

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            VisUtil.ResizeCanvas(Net.Nodes, MainControl, MainModelCanvas);
        }

        public void Deactivate()
        {
        }

        public void UserControlKeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine(e.Key);
            if (e.Key == Key.Delete)
            {
                if (alreadyDeleted == false)
                {
                    btnDelete_Click(sender, e);
                    alreadyDeleted = true;
                }
                else alreadyDeleted = false;
            }

            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                isCtrlPressed = true;
            }

            if (e.Key == Key.C && isCtrlPressed)
            {
                btnCopy_Click(sender, e);
                isCtrlPressed = false;
            }

            if (e.Key == Key.X && isCtrlPressed)
            {
                btnCut_Click(sender, e);
                isCtrlPressed = false;
            }

            if (e.Key == Key.V && isCtrlPressed)
            {
                btnPaste_Click(sender, e);
                isCtrlPressed = false;
            }

            if (e.Key == Key.Z && isCtrlPressed)
            {
                btnUndo_Click(sender, e);
                isCtrlPressed = false;
            }
        }

        private void CanvasSizeChanged(object sender, RoutedEventArgs e)
        {
            //Redraw grid on window resize
            if (_thisScale < 1 || showGrid != false) return;
            RemoveGridFromCanvas();
            AddGridOnCanvas();
        }

        // canvas handler
        private void canvas1_MouseMove(object sender, MouseEventArgs e)
        {
            switch (_leftMouseButtonMode)
            {
                case LeftMouseButtonMode.AddArc:
                    MouseMoveInAddArcMode(e, MainModelCanvas);
                    break;
                case LeftMouseButtonMode.Select:
                    MouseMoveInSelectMode(e, MainModelCanvas);
                    break;
            }
        }

        private void MouseMoveInAddArcMode(MouseEventArgs e, Canvas canvas)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            _lineArcDrawing.Stroke = Brushes.Gray;
            _lineArcDrawing.X2 = e.GetPosition(canvas).X;
            _lineArcDrawing.Y2 = e.GetPosition(canvas).Y;
            Scroll(e);
        }

        private void MouseMoveInSelectMode(MouseEventArgs e, Canvas canvas)
        {
            //todo magic numbers

            if (e.LeftButton != MouseButtonState.Pressed) return;
            _movingHappened = true;

            if (_selecting == false)
            {
                Scroll(e);
                _isFiguresMoving = true;

                if (_mainFigure != null)
                    _mainFigure.IsSelect = true;

                _isCoordinatesNegative = false;
                foreach (PetriNetNode figure in _selectedFigures)
                {
                    if ((e.GetPosition(MainModelCanvas).X - figure.XDistance - 10.0 <= 0.0) ||
                        (e.GetPosition(MainModelCanvas).Y - figure.YDistance - 15.0 <= 0.0))
                    {
                        _isCoordinatesNegative = true;
                    }
                }

                foreach (var f in _selectedFigures)
                {
                    if (_isCoordinatesNegative != false) continue;

                    var figure = GetKeyByValueForFigures(f) as Shape;

                    double coordX, coordY;
                    if (_modeTieToMesh == ModeTieToMesh.NotTie)
                    {
                        coordX = e.GetPosition(canvas).X - f.XDistance - 10.0;
                        coordY = e.GetPosition(canvas).Y - f.YDistance - 15.0;
                    }
                    else
                    {
                        if (figure is Ellipse)
                        {
                            PlaceNodeInTheNearestMesh(e.GetPosition(canvas).X - f.XDistance,
                                e.GetPosition(canvas).Y - f.YDistance,
                                "place", out coordX, out coordY);
                            coordX -= 15.0;
                            coordY -= 15.0;
                        }
                        else
                        {
                            PlaceNodeInTheNearestMesh(e.GetPosition(canvas).X - f.XDistance,
                                e.GetPosition(canvas).Y - f.YDistance,
                                "transition", out coordX, out coordY);

                            coordX -= 5.0;
                            coordY -= 25.0;
                        }
                    }

                    Canvas.SetLeft(figure, coordX);
                    Canvas.SetTop(figure, coordY);

                    f.CoordX = Canvas.GetLeft(figure);
                    f.CoordY = Canvas.GetTop(figure);

                    SetLabel(f);

                    if (f is VPlace)
                    {
                        RemoveTokens(f as VPlace);
                        AddTokens(f as VPlace);
                    }
                }

                foreach (var arc in Net.arcs)
                {
                    var line = GetKeyByValueForArcs(arc, DictionaryForArcs);
                    DictionaryForArcs.Remove(line);
                    canvas.Children.Remove(line);

                    DrawArc(arc);

                    if (arc.WeightLabel != null)
                    {
                        Canvas.SetLeft(arc.WeightLabel, (arc.From.CoordX + arc.To.CoordX) / 2.0);
                        Canvas.SetTop(arc.WeightLabel, (arc.From.CoordY + arc.To.CoordY) / 2.0 - 5.0);
                    }

                    RedrawArrowHeads(arc);
                    ColorArrow(arc);
                }
            }
            else
            {
                var actualX = e.GetPosition(canvas).X;
                var actualY = e.GetPosition(canvas).Y;
                _width = actualX - _selectingXpoint;
                _leftX = _selectingXpoint;
                _height = actualY - _selectingYpoint;
                _topY = _selectingYpoint;
                if (_selectingXpoint > actualX)
                {
                    _width = -_width;
                    _leftX = actualX;
                }

                if (_selectingYpoint > actualY)
                {
                    _height = -_height;
                    _topY = actualY;
                }

                _selectRect.Height = _height;
                _selectRect.Width = _width;

                Canvas.SetLeft(_selectRect, _leftX);
                Canvas.SetTop(_selectRect, _topY);

                _selectRect.Visibility = Visibility.Visible;
                Scroll(e);
            }
        }

        private void canvas1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isFiguresMoving = false;
            _movingHappened = false;
            if (e.ChangedButton == MouseButton.Right)
            {
                if (_leftMouseButtonMode == LeftMouseButtonMode.InsertHandle && _nodeFrom != null)
                {
                    UnselectFigures();
                    _nodeFrom = null;
                    return;
                }
                if (_leftMouseButtonMode != LeftMouseButtonMode.Select)
                {
                    btnSelect_Click(sender, e);
                    return; //do not handle click by buttons
                }
            }

            switch (_leftMouseButtonMode)
            {
                case LeftMouseButtonMode.AddPlace:
                {
                    if (_modeTieToMesh == ModeTieToMesh.NotTie)
                        AddPlace(e.GetPosition(MainModelCanvas).X, e.GetPosition(MainModelCanvas).Y);
                    else
                    {
                        double newX, newY;
                        PlaceNodeInTheNearestMesh(e.GetPosition(MainModelCanvas).X, e.GetPosition(MainModelCanvas).Y,
                            "place", out newX, out newY);
                        AddPlace(newX, newY);
                    }
                }
                    break;
                case LeftMouseButtonMode.AddTransition:
                {
                    if (_modeTieToMesh == ModeTieToMesh.NotTie)
                        AddTransition(e.GetPosition(MainModelCanvas).X, e.GetPosition(MainModelCanvas).Y);
                    else
                    {
                        double newX, newY;
                        PlaceNodeInTheNearestMesh(e.GetPosition(MainModelCanvas).X, e.GetPosition(MainModelCanvas).Y,
                            "transition", out newX, out newY);
                        AddTransition(newX, newY);
                    }
                }
                    break;
                case LeftMouseButtonMode.AddArc:
                {
                    _lineArcDrawing.Visibility = Visibility.Visible;
                    if (e.ButtonState == MouseButtonState.Pressed)
                    {
                        _lineArcDrawing.X1 = e.GetPosition(MainModelCanvas).X;
                        _lineArcDrawing.Y1 = e.GetPosition(MainModelCanvas).Y;
                    }
                }
                    break;
                case LeftMouseButtonMode.Select:
                {
                    _selecting = true;
                    _selectingXpoint = e.GetPosition(MainModelCanvas).X;
                    _selectingYpoint = e.GetPosition(MainModelCanvas).Y;
                    if (isCtrlPressed == false)
                    {
                        UnselectFigures(); //(selectedFigures, selectedArcs);
                        HideAllProperties();
                    }
                }
                    break;
            }
        }

        private void canvas1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Command.CanceledCommands.Count == 0)
                btnRedo.IsEnabled = false;

            _figuresAfterDrag = CopyListOfFigures(Net.Nodes);
            switch (_leftMouseButtonMode)
            {
                case LeftMouseButtonMode.AddPlace:
                {
                    btnAddPlace.Focus();
                }
                    break;
                case LeftMouseButtonMode.AddTransition:
                {
                    btnAddTransition.Focus();
                }
                    break;
                case LeftMouseButtonMode.AddArc:
                {
                    _lineArcDrawing.Visibility = Visibility.Hidden;
                }
                    break;
                case LeftMouseButtonMode.AddToken:
                {
                    btnAddToken.Focus();
                }
                    break;
                case LeftMouseButtonMode.Select:
                {
                    _selectRect.Visibility = Visibility.Hidden;
                    if (_isFiguresMoving)
                    {
                        var newCommand = new DragCommand(_figuresBeforeDrag, _figuresAfterDrag);
                        Command.ExecutedCommands.Push(newCommand);
                        Command.CanceledCommands.Clear();

                        btnUndo.IsEnabled = true;
                    }
                    else
                    {
                        if (_movingHappened == false) //(selecting == false)
                        {
                            if (_selectedFigure != null)
                            {
                                if (_selectedFigure.IsSelect == false)
                                {
                                    _selectedFigures.Remove(_selectedFigure);
                                    if (_selectedFigure is VPlace)
                                    {
                                        var blackEllipse = GetKeyByValueForFigures(_selectedFigure) as Ellipse;
                                        blackEllipse.Stroke = Brushes.Black;
                                    }
                                    else
                                    {
                                        var blackRectangle = GetKeyByValueForFigures(_selectedFigure) as Rectangle;
                                        blackRectangle.Stroke = Brushes.Black;
                                    }

                                    _selectedFigure = null;
                                }
                            }

                            if (_selectedArc != null)
                            {
                                if (_selectedArc.IsSelect == false)
                                {
                                    _selectedArcs.Remove(_selectedArc);
                                    ColorArrow(_selectedArc);
                                    _selectedArc = null;
                                }
                            }

                            ReassignSelectedProperties();
                        }
                        else
                        {
                            foreach (var figure in Net.Nodes)
                            {
                                var coordX = figure.CoordX;
                                double coordY = figure.CoordY;
                                int x, y;
                                if (figure is VPlace)
                                {
                                    x = y = 30;
                                }
                                else
                                {
                                    x = 20;
                                    y = 50;
                                }

                                if (coordX + x > _leftX && coordX < _leftX + _width && coordY < _topY + _height &&
                                    coordY + y > _topY)
                                {
                                    figure.IsSelect = true;
                                    MakeSelected(figure);
                                }
                                else
                                {
                                    figure.IsSelect = false;
                                    (GetKeyByValueForFigures(figure) as Shape).Stroke = Brushes.Black;
                                    _selectedFigures.Remove(figure);
                                }
                            }

                            foreach (var arc in Net.arcs)
                            {
                                if (_selectedFigures.Contains(arc.From) && _selectedFigures.Contains(arc.To))
                                {
                                    arc.IsSelect = true;
                                    _selectedArcs.Add(arc);
                                }
                                else
                                {
                                    arc.IsSelect = false;
                                    _selectedArcs.Remove(arc);
                                }

                                ColorArrow(arc);
                            }

                            ReassignSelectedProperties();
                        }

                        _selecting = false;
                    }

                    VisUtil.ResizeCanvas(Net.Nodes, MainControl, MainModelCanvas);
                }
                    break;
            }
        }

        private void canvas1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!isCtrlPressed) return;

            var canvas = sender as Canvas;
            if (e.Delta > 0)
            {
                _scaleTransform.ScaleX *= ScaleRate;
                _scaleTransform.ScaleY *= ScaleRate;
                _thisScale *= ScaleRate;
            }
            else
            {
                _scaleTransform.ScaleX /= ScaleRate;
                _scaleTransform.ScaleY /= ScaleRate;
                _thisScale /= ScaleRate;
            }

            Debug.Assert(canvas != null, "Canvas != null in canvas1_MouseWheel");
            canvas.LayoutTransform = _scaleTransform;
            canvas.UpdateLayout();

            VisUtil.ResizeCanvas(Net.Nodes, MainControl, MainModelCanvas);

            HideOrShowGrid(_thisScale);
            TurnOnSelectMode();
        }

        private void MousePlaceDown(object sender, RoutedEventArgs e)
        {
            _isFiguresMoving = false;
            _figuresBeforeDrag = CopyListOfFigures(Net.Nodes);
            switch (_leftMouseButtonMode)
            {
                case LeftMouseButtonMode.AddArc:
                {
                    _isFigurePressed = true;
                    _tempFrom = GetSenderPlace(sender);
                }
                    break;
                case LeftMouseButtonMode.Select:
                {
                    _selecting = false;
                    if (_isFiguresMoving == false)
                    {
                        _selectedFigure = GetSenderPlace(sender);
                        _mainFigure = _selectedFigure;
                        _numberOfTokensChanged = (_selectedFigure as VPlace).NumberOfTokens;
                        SelectOrUnselectFigure();
                        MakeSelected(_selectedFigure);
                        CountDistancesBetweenTheDruggedNodeAndOtherNodes();
                    }

                    e.Handled = true;
                }
                    break;
                case LeftMouseButtonMode.AddToken:
                {
                    _markedPlace = GetSenderPlace(sender);
                    _markedPlace.NumberOfTokens += 1;
                    //if (_selectedFigure == _markedPlace)
                    //    tbTokenNumber.Text = _markedPlace.NumberOfTokens.ToString();
                    if (_markedPlace.NumberOfTokens == 0)
                        RemoveTokens(_markedPlace);
                    if (_markedPlace.NumberOfTokens >= 0 && _markedPlace.NumberOfTokens < 5)
                        MainModelCanvas.Children.Remove(_markedPlace.NumberOfTokensLabel);
                    AddTokens(_markedPlace);
                    AddTokensCommand newCommand = new AddTokensCommand(_markedPlace, _markedPlace.NumberOfTokens - 1,
                        _markedPlace.NumberOfTokens);
                    Command.ExecutedCommands.Push(newCommand);
                    Command.CanceledCommands.Clear();
                    e.Handled = true;
                }
                    break;
                case LeftMouseButtonMode.InsertHandle:
                {
                    // если выбираем вторую вершину
                    if (_nodeFrom != null)
                    {
                        _nodeTo = GetSenderPlace(sender);
                        _nodeTo.IsSelect = true;
                        MakeSelected(_nodeTo);
                        InsertHandle(_nodeFrom, _nodeTo);
                    }
                    // если выбираем первую вершину
                    else
                    {
                        UnselectFigures();
                        _nodeFrom = GetSenderPlace(sender);
                        _nodeFrom.IsSelect = true;
                        MakeSelected(_nodeFrom);
                    }
                }
                    break;
            }
        }

        private void MousePlaceUp(object sender, RoutedEventArgs e)
        {
            switch (_leftMouseButtonMode)
            {
                case LeftMouseButtonMode.AddArc:
                {
                    if (_isFigurePressed)
                    {
                        _isFigurePressed = false;
                        _tempTo = GetSenderPlace(sender);
                        if (_tempFrom is VPlace)
                            MessageBox.Show(ARCSBETWEENPLACES);
                        else
                        {
                            VArc newArc = AddArc(_tempFrom, _tempTo);
                            AddArcCommand newCommand = new AddArcCommand(newArc);
                            Command.ExecutedCommands.Push(newCommand);
                            Command.CanceledCommands.Clear();
                        }
                    }
                }
                    break;
            }
        }

        private void MouseTransitionDown(object sender, MouseButtonEventArgs e)
        {
            _isFiguresMoving = false;
            _figuresBeforeDrag = CopyListOfFigures(Net.Nodes);
            switch (_leftMouseButtonMode)
            {
                case LeftMouseButtonMode.AddArc:
                {
                    _isFigurePressed = true;
                    _allFiguresObjectReferences.TryGetValue(sender, out _tempFrom);
                }
                    break;
                case LeftMouseButtonMode.Select:
                {
                    _selecting = false;
                    if (_isFiguresMoving == false)
                    {
                        _allFiguresObjectReferences.TryGetValue(sender, out _selectedFigure);
                        _mainFigure = _selectedFigure;
                        SelectOrUnselectFigure();
                        MakeSelected(_selectedFigure);
                        CountDistancesBetweenTheDruggedNodeAndOtherNodes();
                    }

                    e.Handled = true;
                }
                    break;
                case LeftMouseButtonMode.InsertHandle:
                {
                    // если выбираем вторую вершину
                    if (_nodeFrom != null)
                    {
                        _allFiguresObjectReferences.TryGetValue(sender, out _nodeTo);
                        MakeSelected(_nodeTo);
                        InsertHandle(_nodeFrom, _nodeTo);
                    }
                    // если выбираем первую вершину
                    else
                    {
                         UnselectFigures();
                        _allFiguresObjectReferences.TryGetValue(sender, out _nodeFrom);
                        _nodeFrom.IsSelect = true;
                        MakeSelected(_nodeFrom);
                    }
                }
                    break;
            }
        }

        private void MouseTransitionUp(object sender, RoutedEventArgs e)
        {
            switch (_leftMouseButtonMode)
            {
                case LeftMouseButtonMode.AddArc:
                {
                    if (_isFigurePressed)
                    {
                        _isFigurePressed = false;
                        _allFiguresObjectReferences.TryGetValue(sender, out _tempTo);
                        if (_tempFrom != _tempTo)
                        {
                            if (_tempFrom is VTransition)
                                MessageBox.Show(ARCSBETWEENTRANSITIONS);
                            else
                            {
                                VArc newArc = AddArc(_tempFrom, _tempTo);
                                AddArcCommand newCommand = new AddArcCommand(newArc);
                                Command.ExecutedCommands.Push(newCommand);
                                Command.CanceledCommands.Clear();
                            }
                        }
                    }
                }
                    break;
            }
        }

        private void MouseArcDown(object sender, MouseButtonEventArgs e)
        {
            if (_leftMouseButtonMode != LeftMouseButtonMode.Select) return;
            if (DictionaryForArcs.ContainsKey(sender as Shape))
                _tempDictionary = DictionaryForArcs;
            else if (DictionaryForArrowHeads1.ContainsKey(sender as Shape))
                _tempDictionary = DictionaryForArrowHeads1;
            else
                _tempDictionary = DictionaryForArrowHeads2;
            _tempDictionary.TryGetValue((sender as Shape), out _selectedArc);

            if (isCtrlPressed == false && (_selectedFigures.Count + _selectedArcs.Count != 0) &&
                _selectedArc.IsSelect == false)
            {
                UnselectFigures(); //(selectedFigures, selectedArcs);
            }

            _selectedArc.IsSelect = _selectedArc.IsSelect == false;

            if (_selectedArc.IsSelect)
            {
                _selectedArcs.Add(_selectedArc);
                ColorArrow(_selectedArc);
                ReassignSelectedProperties();
            }

            e.Handled = true;
        }

        private void MainTabControlWithModelCanvases_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //todo: this does not matter, no tabs in future
        }


        double arcWeigth;

        bool isArcWeightInt;
        //todo What is it?

        #endregion Events 

        private static readonly Stopwatch Stopwatch = new Stopwatch();

        public void StopStopWatch()
        {
            Stopwatch.Stop();
        }

        public delegate void ShowNameDelegate(string name);

        public static ShowNameDelegate ShowMainWindowTitleDelegate;

        private readonly Line[] _arrowHeads = new Line[2];

        private static readonly Random MainRandom = new Random(); // main random

        private enum LeftMouseButtonMode
        {
            Select,
            Delete,
            AddPlace,
            AddTransition,
            AddArc,
            AddToken,
            InsertHandle,
            ChooseTransition,
            SetInitialState,
            AddFinalState,
            PriorityUp,
            PriorityDown
        };

        private LeftMouseButtonMode _leftMouseButtonMode = LeftMouseButtonMode.Select;
        //TODO закрыть

        private enum Choice
        {
            Forced,
            Random
        };

        private Choice _modeChoice = Choice.Random;

        private Line _lineArcDrawing = new Line();

        private bool _selecting;
        private double _selectingXpoint, _selectingYpoint;

        private PetriNetNode _tempFrom, _tempTo, _selectedFigure;

        // переменные для вставки ручек
        private PetriNetNode _nodeFrom = null;
        private PetriNetNode _nodeTo = null;

        private VPlace _markedPlace;
        private VTransition _markedTransition;
        private bool _isFigurePressed;
        private readonly List<PetriNetNode> _selectedFigures = new List<PetriNetNode>();
        private PetriNetNode _mainFigure;

        private readonly Dictionary<VPlace, Ellipse> _finalStates = new Dictionary<VPlace, Ellipse>();

        private VArc _selectedArc;
        private readonly List<VArc> _selectedArcs = new List<VArc>();
        private Dictionary<Shape, VArc> _tempDictionary;

        private bool _isTransitionSelected;
        private int _numberOfTokensChanged;
        private bool _isNumberOfTokensInt;
        private int _priorityChanged;
        private bool _isPriorityInt;

        private bool _isCoordinatesNegative;
        private bool _isFiguresMoving;
        private readonly Rectangle _selectRect = new Rectangle();
        private double _leftX, _topY, _width, _height;
        private bool _movingHappened;

        private enum ModeTieToMesh
        {
            Tie,
            NotTie
        }

        private ModeTieToMesh _modeTieToMesh = ModeTieToMesh.NotTie;
        private bool _tie;


        private readonly Rectangle _outline = new Rectangle();

        private List<PetriNetNode> _figuresBeforeDrag = new List<PetriNetNode>();
        private List<PetriNetNode> _figuresAfterDrag = new List<PetriNetNode>();

        private readonly Dictionary<Grid, Polyline> _syncRelationsDictionary = new Dictionary<Grid, Polyline>();

        private int _currentRow;

        public enum SyncModeEnum
        {
            Sync,
            Async
        };

        public static SyncModeEnum SyncMode;


        private bool _firstFocusOnTab = true;

        private void AddSelectRectangeOnCanvas(Rectangle selectedRect)
        {
            selectedRect.Stroke = Brushes.Blue;
            selectedRect.StrokeThickness = 1;
            SolidColorBrush brush = new SolidColorBrush(Colors.Blue) {Opacity = 0.05};
            selectedRect.Fill = brush;
            selectedRect.Visibility = Visibility.Hidden;
            selectedRect.StrokeDashArray = new DoubleCollection() {3};
            MainModelCanvas.Children.Add(selectedRect);
        }

        private void DeleteArcs(IList<VArc> arcs)
        {
            foreach (VArc arc in arcs)
            {
                Shape nextArcForRemove = GetKeyByValueForArcs(arc, DictionaryForArcs);
                DictionaryForArcs.Remove(nextArcForRemove);
                nextArcForRemove.Stroke = Brushes.Black;
                MainModelCanvas.Children.Remove(nextArcForRemove);

                if (arc.IsDirected)
                {
                    nextArcForRemove = GetKeyByValueForArcs(arc, DictionaryForArrowHeads1);
                    MainModelCanvas.Children.Remove(nextArcForRemove);
                    nextArcForRemove.Stroke = Brushes.Black;
                    nextArcForRemove = GetKeyByValueForArcs(arc, DictionaryForArrowHeads2);
                    MainModelCanvas.Children.Remove(nextArcForRemove);
                    nextArcForRemove.Stroke = Brushes.Black;
                }

                arc.IsSelect = false;
                
                if (arc.WeightLabel != null)
                    MainModelCanvas.Children.Remove(arc.WeightLabel);
            }

            Net.DeleteArcs(arcs);
        }

        private void DeleteFigures(List<PetriNetNode> selectedF, List<VArc> selectedA)
        {
            _leftMouseButtonMode = LeftMouseButtonMode.Delete;
            EnableAddButtons();

            DeleteArcs(selectedA);

            if (selectedF.Count != 0)
            {
                foreach (PetriNetNode figure in selectedF)
                {
                    RemoveNode(figure);
                }
            }

            ReassignSelectedProperties();

            _selectedFigure = null;
            _selectedArc = null;
            _nodeFrom = null;
            TurnOnSelectMode();
        }

        private void MakeDeleteCommand(List<PetriNetNode> selectedF, List<VArc> selectedA)
        {
            List<PetriNetNode> deletedFigures = new List<PetriNetNode>();
            List<VArc> deletedArcs = new List<VArc>();
            foreach (PetriNetNode figure in selectedF)
            {
                deletedFigures.Add(figure);
                foreach (VArc arc in figure.ThisArcs)
                    if (!deletedArcs.Contains(arc))
                        deletedArcs.Add(arc);
            }

            foreach (VArc arc in selectedA)
            {
                if (!deletedArcs.Contains(arc))
                    deletedArcs.Add(arc);
            }

            DeleteCommand newCommand = new DeleteCommand(deletedFigures, deletedArcs);
            Command.ExecutedCommands.Push(newCommand);
            Command.CanceledCommands.Clear();
        }

        private void AddTokens(VPlace changedPlace)
        {
            switch (changedPlace.NumberOfTokens)
            {
                case 0:
                    break;
                case 1:
                    AddOneToken(changedPlace);
                    break;
                case 2:
                    AddTwoTokens(changedPlace);
                    break;
                case 3:
                    AddThreeTokens(changedPlace);
                    break;
                case 4:
                    AddFourTokens(changedPlace);
                    break;
                case 5:
                    AddFiveTokens(changedPlace, changedPlace.NumberOfTokensLabel);
                    break;
                default:
                    if (changedPlace.NumberOfTokens >= 6 && changedPlace.NumberOfTokens <= 999)
                    {
                        AddFrom6To999Tokens(changedPlace, changedPlace.NumberOfTokensLabel);
                    }
                    else
                    {
                        AddMoreThan1000Tokens(changedPlace, changedPlace.NumberOfTokensLabel);
                    }

                    break;
            }

            if (changedPlace.NumberOfTokens > 0 && changedPlace.NumberOfTokens < 5)
            {
                foreach (Ellipse ellipse in changedPlace.TokensList)
                {
                    ellipse.AddHandler(MouseLeftButtonDownEvent, new RoutedEventHandler(MousePlaceDown));
                    ellipse.AddHandler(MouseLeftButtonUpEvent, new RoutedEventHandler(MousePlaceUp));
                }
            }
            else if (changedPlace.NumberOfTokens >= 5)
            {
                changedPlace.NumberOfTokensLabel.AddHandler(MouseLeftButtonDownEvent,
                    new RoutedEventHandler(MousePlaceDown));

                changedPlace.NumberOfTokensLabel.AddHandler(MouseLeftButtonUpEvent,
                    new RoutedEventHandler(MousePlaceUp));
            }
        }

        private void AddFigure(double coordX, double coordY, Shape temp)
        {
            int w, h;
            if (temp is Rectangle)
            {
                w = TRANSITIONWIDTH;
                h = TRANSITIONHEIGHT;
            }
            else
            {
                w = PLACEWIDTH;
                h = PLACEHEIGHT;
            }

            temp.Width = w;
            temp.Height = h;
            SolidColorBrush brush = new SolidColorBrush(Colors.White) {Opacity = 0};
            temp.Fill = brush;
            temp.StrokeThickness = 2;
            temp.Stroke = Brushes.Black;
            temp.Visibility = Visibility.Visible;
            if (coordX > MainModelCanvas.ActualWidth - w)
                MainModelCanvas.Width = MainModelCanvas.ActualWidth + PLACEWIDTH * 5;
            else if (coordY > MainModelCanvas.ActualHeight - h)
                MainModelCanvas.Height = MainModelCanvas.ActualHeight + PLACEHEIGHT * 5;
            if (coordX - w / 2 > 0)
                coordX -= w / 2;
            else
                coordX = 0;
            Canvas.SetLeft(temp, coordX);
            if (coordY - h / 2 > 0)
                coordY -= h / 2;
            else
                coordY = 0;
            Canvas.SetTop(temp, coordY);

            PetriNetNode newFigure;
            if (temp is Rectangle)
            {
                newFigure = VTransition.Create(coordX, coordY);
                Net.transitions.Add((VTransition) newFigure);
                temp.MouseDown += MouseTransitionDown;
                temp.MouseUp += MouseTransitionUp;
            }
            else
            {
                newFigure = VPlace.Create(coordX, coordY);
                Net.places.Add((VPlace) newFigure);
                temp.MouseDown += MousePlaceDown;
                temp.MouseUp += MousePlaceUp;
            }


            //SetOfFigures.Figures.Add(newFigure);
            _allFiguresObjectReferences.Add(temp, newFigure);

            newFigure.DetectIdMatches(Net.Nodes);
            MainModelCanvas.Children.Add(temp);
            btnUndo.IsEnabled = true;

            AddFigureCommand newCommand = new AddFigureCommand(newFigure, temp);
            Command.ExecutedCommands.Push(newCommand);
            Command.CanceledCommands.Clear();
        }

        private void AddPlace(double coordX, double coordY)
        {
            AddFigure(coordX, coordY, new Ellipse());
        }

        private void AddTransition(double coordX, double coordY)
        {
            AddFigure(coordX, coordY, new Rectangle());
        }

        private void InsertHandle(PetriNetNode first_node, PetriNetNode last_node)
        {
            UnselectFigures();
            // PP or TT handle
            if (first_node is VPlace && last_node is VPlace
                || first_node is VTransition && last_node is VTransition)
            {
                List<PetriNetNode> requiredNodes = new List<PetriNetNode>();
                requiredNodes.Add(first_node);
                requiredNodes.Add(last_node);
                GraphUtil.Prepare(Net, requiredNodes);
                bool isCanInsert = false;
                GraphUtil.DepthFirstSearchForCycles(first_node, ref isCanInsert);
                if (!isCanInsert)
                {
                    MessageBoxResult result = MessageBox.Show("Inserting a handle can lead to unboundedness or non-liveness of net.\nInsert anyway?",
                        "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                        insert_PP_or_TT_Handle(first_node, last_node);
                }
                // первый этап проверки пройден
                else
                {
                    // PP handle
                    if (first_node is VPlace && last_node is VPlace)
                        // для PP 1 этапа достаточно
                        insert_PP_or_TT_Handle(first_node, last_node);
                    // TT handle
                    else
                    {
                        List<PetriNetNode> handle = add_TT_HandleToModel(first_node, last_node);
                        bool boundedness = true;
                        bool isTransitionLive1 = false;
                        CoverabilityTree.Create(Net, first_node as VTransition, ref boundedness, ref isTransitionLive1);
                        remove_TT_HandleFromModel(first_node, last_node, handle);
                        if (isTransitionLive1)
                            insert_PP_or_TT_Handle(first_node, last_node);
                        else
                        {
                            MessageBoxResult result = MessageBox.Show("Inserting a handle can lead to unboundedness or non-liveness of net.\nInsert anyway?",
                                "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (result == MessageBoxResult.Yes)
                                insert_PP_or_TT_Handle(first_node, last_node);
                        }
                    }
                }

            }
            // TP or PT handle
            else
            {
                // TP handle
                if (first_node is VTransition && last_node is VPlace)
                {
                    bool boundedness = true;
                    bool isTranstionLive1 = true;
                    CoverabilityTree.Create(Net, null, ref boundedness, ref isTranstionLive1);
                    if (boundedness)
                    {
                        List<PetriNetNode> handle = add_TP_HandleToModel(first_node, last_node);
                        CoverabilityTree.Create(Net, null, ref boundedness, ref isTranstionLive1);
                        remove_TP_HandleFromModel(first_node, last_node, handle);
                        if (boundedness)
                            insert_TP_or_PT_Handle(first_node, last_node);
                        else
                        {
                            MessageBoxResult result = MessageBox.Show("Inserting a handle will result in unboundedness net.\nInsert anyway?",
                                "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (result == MessageBoxResult.Yes)
                                insert_TP_or_PT_Handle(first_node, last_node);
                        }
                    }
                    else
                        insert_TP_or_PT_Handle(first_node, last_node);
                }
                // PT handle
                else
                {
                    MessageBoxResult result = MessageBox.Show("Inserting a handle can lead to unboundedness or non-liveness of net.\nInsert anyway?",
                        "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                        insert_TP_or_PT_Handle(first_node, last_node);
                }
            }
            //CoverabilityTree.writer.Flush();
            //CoverabilityTree.writer.Close();

            _nodeFrom = null;
            _nodeTo = null;
        }

        private List<PetriNetNode> add_TP_HandleToModel(PetriNetNode first_node, PetriNetNode last_node)
        {
            List<PetriNetNode> handle = new List<PetriNetNode>();
            VPlace place = new VPlace(0, 0, 0, "");
            Net.places.Add(place);
            VTransition transition = new VTransition(0, 0, "");
            Net.transitions.Add(transition);
            handle.Add(place);
            handle.Add(transition);

            addArcsToModel(first_node, place);
            addArcsToModel(place, transition);
            addArcsToModel(transition, last_node);

            return handle;
        }

        private List<PetriNetNode> add_TT_HandleToModel(PetriNetNode first_node, PetriNetNode last_node)
        {
            List<PetriNetNode> handle = new List<PetriNetNode>();
            VPlace place = new VPlace(0, 0, 0, "");
            Net.places.Add(place);
            handle.Add(place);

            addArcsToModel(first_node, place);
            addArcsToModel(place, last_node);

            return handle;
        }

        private void remove_TT_HandleFromModel(PetriNetNode first_node, PetriNetNode last_node, List<PetriNetNode> handle)
        {
            Net.places.RemoveAt(Net.places.Count - 1);
            Net.arcs.RemoveAt(Net.arcs.Count - 1);
            first_node.ThisArcs.RemoveAt(first_node.ThisArcs.Count - 1);
            last_node.ThisArcs.RemoveAt(last_node.ThisArcs.Count - 1);
            foreach (PetriNetNode node in handle)
            {
                Net.arcs.RemoveAt(Net.arcs.Count - 1);
                node.ThisArcs.RemoveAt(node.ThisArcs.Count - 1);
                node.ThisArcs.RemoveAt(node.ThisArcs.Count - 1);
            }
        }

        private void remove_TP_HandleFromModel(PetriNetNode first_node, PetriNetNode last_node, List<PetriNetNode> handle)
        {
            Net.places.RemoveAt(Net.places.Count - 1);
            Net.transitions.RemoveAt(Net.transitions.Count - 1);
            Net.arcs.RemoveAt(Net.arcs.Count - 1);
            first_node.ThisArcs.RemoveAt(first_node.ThisArcs.Count - 1);
            last_node.ThisArcs.RemoveAt(last_node.ThisArcs.Count - 1);
            foreach (PetriNetNode node in handle)
            {
                Net.arcs.RemoveAt(Net.arcs.Count - 1);
                node.ThisArcs.RemoveAt(node.ThisArcs.Count - 1);
                node.ThisArcs.RemoveAt(node.ThisArcs.Count - 1);
            }
        }

        private void addArcsToModel(PetriNetNode from, PetriNetNode to)
        {
            VArc newArc = new VArc(from, to);
            Net.arcs.Add(newArc);
            from.ThisArcs.Add(newArc);
            to.ThisArcs.Add(newArc);
        }

        private void insert_PP_or_TT_Handle(PetriNetNode first_node, PetriNetNode last_node)
        {
            List<Shape> shapes = new List<Shape>();
            int sign_delta_x, sign_delta_y;
            double normal_k;
            double sqr_delta_x, sqr_delta_y;
            calcCoefficients(first_node, last_node, out sign_delta_x, out sign_delta_y, out normal_k, out sqr_delta_x, out sqr_delta_y);
            double middle_x = (first_node.CoordX + last_node.CoordX) / 2 + PLACEWIDTH / 2;
            double middle_y = (first_node.CoordY + last_node.CoordY) / 2 + PLACEHEIGHT / 2;
            // PP handle
            if (first_node is VPlace && last_node is VPlace)
                shapes.Add(new Rectangle());

            // TT handle
            else
                shapes.Add(new Ellipse());

            if (sign_delta_x == 0 && sign_delta_y == 0)
            {
                AddFigure(middle_x + DISTANCE, middle_y + DISTANCE, shapes[0]);
                LinkAndSelectNodes(first_node, last_node, shapes);
                return;
            }
            if (sign_delta_y == 0)
            {
                AddFigure(middle_x, middle_y + sign_delta_x * DISTANCE, shapes[0]);
                LinkAndSelectNodes(first_node, last_node, shapes);
                return;
            }
            else if (sign_delta_x == 0)
            {
                AddFigure(middle_x + sign_delta_y * DISTANCE, middle_y, shapes[0]);
                LinkAndSelectNodes(first_node, last_node, shapes);
                return;
            }
            AddFigure(middle_x + sign_delta_y * Math.Sqrt(sqr_delta_x),
                middle_y + sign_delta_x * Math.Sqrt(sqr_delta_y), shapes[0]);
            LinkAndSelectNodes(first_node, last_node, shapes);
        }

        private void insert_TP_or_PT_Handle(PetriNetNode first_node, PetriNetNode last_node)
        {
            List<Shape> shapes = new List<Shape>();
            int sign_delta_x, sign_delta_y;
            double normal_k;
            double sqr_delta_x, sqr_delta_y;
            calcCoefficients(first_node, last_node, out sign_delta_x, out sign_delta_y, out normal_k, out sqr_delta_x, out sqr_delta_y);

            // TP handle
            if (first_node is VTransition && last_node is VPlace)
            {
                shapes.Add(new Ellipse());
                shapes.Add(new Rectangle());
            }
            // PT handle
            else
            {
                shapes.Add(new Rectangle());
                shapes.Add(new Ellipse());
            }

            if (sign_delta_x == 0 && sign_delta_y == 0)
            {
                AddFigure(first_node.CoordX + PLACEWIDTH / 2 + DISTANCE,
                    first_node.CoordY + PLACEHEIGHT / 2 + DISTANCE, shapes[0]);
                AddFigure(last_node.CoordX + PLACEWIDTH / 2 + DISTANCE,
                    last_node.CoordY + PLACEHEIGHT / 2 + DISTANCE, shapes[1]);
                LinkAndSelectNodes(first_node, last_node, shapes);
                return;
            }
            if (sign_delta_y == 0)
            {
                AddFigure(first_node.CoordX + PLACEWIDTH / 2,
                    first_node.CoordY + PLACEHEIGHT / 2 + sign_delta_x * DISTANCE, shapes[0]);
                AddFigure(last_node.CoordX + PLACEWIDTH / 2,
                    last_node.CoordY + PLACEHEIGHT / 2 + sign_delta_x * DISTANCE, shapes[1]);
                LinkAndSelectNodes(first_node, last_node, shapes);
                return;
            }
            else if (sign_delta_x == 0)
            {
                AddFigure(first_node.CoordX + PLACEWIDTH / 2 + sign_delta_y * DISTANCE,
                    first_node.CoordY + PLACEHEIGHT / 2, shapes[0]);
                AddFigure(last_node.CoordX + PLACEWIDTH / 2 + sign_delta_y * DISTANCE,
                    last_node.CoordY + PLACEHEIGHT / 2, shapes[1]);
                LinkAndSelectNodes(first_node, last_node, shapes);
                return;
            }
            AddFigure(first_node.CoordX + PLACEWIDTH / 2 + sign_delta_y * Math.Sqrt(sqr_delta_x),
                first_node.CoordY + PLACEHEIGHT / 2 + sign_delta_x * Math.Sqrt(sqr_delta_y), shapes[0]);
            AddFigure(last_node.CoordX + PLACEWIDTH / 2 + sign_delta_y * Math.Sqrt(sqr_delta_x),
                last_node.CoordY + PLACEHEIGHT / 2 + sign_delta_x * Math.Sqrt(sqr_delta_y), shapes[1]);
            LinkAndSelectNodes(first_node, last_node, shapes);
        }

        private void calcCoefficients(PetriNetNode first_node, PetriNetNode last_node, out int sign_delta_x,
                 out int sign_delta_y, out double normal_k, out double sqr_delta_x, out double sqr_delta_y)
        {
            sign_delta_x = getSign(first_node.CoordX - last_node.CoordX);
            sign_delta_y = getSign(last_node.CoordY - first_node.CoordY);
            // y = kx + b_1 уравнение прямой, задаваемой 2 вершинами
            // y = normal_k * x + b_2 уравнение прямой, перпендикулярной предыдущей
            normal_k = -(last_node.CoordX - first_node.CoordX) / (last_node.CoordY - first_node.CoordY);
            normal_k *= normal_k;
            sqr_delta_x = DISTANCE * DISTANCE / (1 + normal_k);
            sqr_delta_y = sqr_delta_x * normal_k;
        }

        private void LinkAndSelectNodes(PetriNetNode first_node, PetriNetNode last_node, List<Shape> shapes)
        {
            PetriNetNode previous_node, current_node;
            _allFiguresObjectReferences.TryGetValue(shapes[0], out current_node);
            btnAddArc.IsEnabled = false;
            VArc temp_arc = AddArc(first_node, current_node);
            temp_arc.IsSelect = true;
            _selectedArcs.Add(temp_arc);
            ColorArrow(temp_arc);
            for (int i = 1; i < shapes.Count; ++i)
            {
                _allFiguresObjectReferences.TryGetValue(shapes[i - 1], out previous_node);
                _allFiguresObjectReferences.TryGetValue(shapes[i], out current_node);
                previous_node.IsSelect = true;
                MakeSelected(previous_node);
                temp_arc = AddArc(previous_node, current_node);
                temp_arc.IsSelect = true;
                _selectedArcs.Add(temp_arc);
                ColorArrow(temp_arc);
            }
            current_node.IsSelect = true;
            MakeSelected(current_node);
            temp_arc = AddArc(current_node, last_node);
            temp_arc.IsSelect = true;
            _selectedArcs.Add(temp_arc);
            ColorArrow(temp_arc);
            btnAddArc.IsEnabled = true;
            ReassignSelectedProperties();
        }

        private int getSign(double value)
        {
            if (Math.Abs(value) < EPSILON)
                return 0;
            if(value < 0)
                return -1;
            return 1;
        }

        public void PlaceNodeInTheNearestMesh(double coordX, double coordY, string figure, out double x, out double y)
        {
            int column = (int) (coordX / PETRINETCELLWIDTH) + 1;
            int row = (int) (coordY / PETRINETCELLHEIGHT) + 1;
            double newX;
            double newY;
            if (figure == "place")
            {
                newX = 10 + PETRINETCELLWIDTH * (column - 1) + PLACEWIDTH / 2;
                newY = 20 + PETRINETCELLHEIGHT * (row - 1) + PLACEHEIGHT / 2;
            }
            else
            {
                newX = 10 + PETRINETCELLWIDTH * (column - 1) + TRANSITIONWIDTH / 2;
                newY = 10 + PETRINETCELLHEIGHT * (row - 1) + TRANSITIONHEIGHT / 2;
            }

            x = newX;
            y = newY;
        }

        private VPlace GetSenderPlace(object sender)
        {
            PetriNetNode selected = null;
            if (sender is Label)
            {
                foreach (VPlace place in Net.places)
                    if (((Label) sender) == place.NumberOfTokensLabel)
                        selected = place;
            }
            else
            {
                Ellipse _selectedEllipse = _selectedEllipse = sender as Ellipse;
                if (_selectedEllipse != null && _selectedEllipse.Height < 30)
                {
                    foreach (VPlace place in Net.places)
                        if (place.TokensList.Contains(_selectedEllipse))
                            selected = place;
                }
                else
                {
                    _allFiguresObjectReferences.TryGetValue(sender, out selected);
                }
            }

            return selected as VPlace;
        }

        private void MakeSelected(PetriNetNode figure)
        {
            if (_selectedFigures.Contains(figure)) return;
            if (!figure.IsSelect) return;
            _selectedFigures.Add(figure);
            var ellipseForColor = GetKeyByValueForFigures(figure) as Shape;
            if (ellipseForColor != null) ellipseForColor.Stroke = Brushes.Chocolate;
            ReassignSelectedProperties();
        }

        private List<PetriNetNode> CopyListOfFigures(List<PetriNetNode> toBeCopied)
        {
            List<PetriNetNode> list = new List<PetriNetNode>();
            foreach (var figure in toBeCopied)
            {
                VPlace place = figure as VPlace;
                list.Add((place != null) ? (PetriNetNode) place.Copy() : ((VTransition) figure).Copy());
            }

            return list;
        }

        private void UnselectFigures() //(List<PetriNetNode> selectedF, List<Arc> selectedA)
        {
            foreach (PetriNetNode figure in _selectedFigures)
            {
                figure.IsSelect = false;
                var shape = GetKeyByValueForFigures(figure) as Shape;
                if (shape != null)
                    shape.Stroke = Brushes.Black;
            }

            foreach (VArc arc in _selectedArcs)
            {
                arc.IsSelect = false;
                (GetKeyByValueForArcs(arc, DictionaryForArcs)).Stroke = Brushes.Black;
                if (arc.IsDirected)
                {
                    (GetKeyByValueForArcs(arc, DictionaryForArrowHeads1)).Stroke = Brushes.Black;
                    (GetKeyByValueForArcs(arc, DictionaryForArrowHeads2)).Stroke = Brushes.Black;
                }
            }

            _selectedFigures.Clear();
            _selectedArcs.Clear();
        }

        private void ChangeCoordinatedOfWeightLabel(VArc arc)
        {
            if (arc.WeightLabel != null)
            {
                Canvas.SetLeft(arc.WeightLabel, (arc.From.CoordX + arc.To.CoordX) / 2);
                Canvas.SetTop(arc.WeightLabel, (arc.From.CoordY + arc.To.CoordY) / 2 - 5);
            }
        }

        private void SelectOrUnselectFigure()
        {
            if (isCtrlPressed == false && (_selectedFigures.Count +
                                           _selectedArcs.Count != 0) && _selectedFigure.IsSelect == false)
            {
                UnselectFigures(); //(selectedFigures, selectedArcs);
            }

            _selectedFigure.IsSelect = !_selectedFigures.Contains(_selectedFigure);
        }

        private void CountDistancesBetweenTheDruggedNodeAndOtherNodes()
        {
            foreach (PetriNetNode figure in _selectedFigures)
            {
                figure.XDistance = _mainFigure.CoordX - figure.CoordX;
                figure.YDistance = _mainFigure.CoordY - figure.CoordY;
            }
        }

        private void ChangeLabel(PetriNetNode figure, string text)
        {
            Label label;
            NodesToLabelsInCanvas.TryGetValue(figure, out label);

            MainModelCanvas.Children.Remove(label);
            NodesToLabelsInCanvas.Remove(figure);

            AddLabel(figure, text);
        }

        private void AddLabel(PetriNetNode figure, String label)
        {
            figure.Label = label;
            var permanentLabel = new Label {Content = figure.Label};

            NodesToLabelsInCanvas.Add(figure, permanentLabel);

            if (figure is VTransition)
            {
                Canvas.SetLeft(permanentLabel, figure.CoordX + 5.0 - label.Length * 8.0 / 2.0);
                Canvas.SetTop(permanentLabel, figure.CoordY + 45.0);
            }
            else
            {
                Canvas.SetLeft(permanentLabel, figure.CoordX + 10.0 - label.Length * 8.0 / 2.0);
                Canvas.SetTop(permanentLabel, figure.CoordY + 25.0);
            }

            MainModelCanvas.Children.Add(permanentLabel);

            if (Canvas.GetLeft(permanentLabel) < 0)
                Canvas.SetLeft(permanentLabel, 0);
        }

        private VArc AddArc(PetriNetNode from, PetriNetNode to)
        {
            var newArc = new VArc(from, to);

            newArc.DetectIdMatches(Net.arcs);

            if (btnAddArc.IsEnabled == false)
                newArc.IsDirected = true;
            else if (btnNonOrientedArc.IsEnabled == false)
                newArc.IsDirected = false;

            if (DoesArcAlreadyExist(newArc, Net.arcs)) return newArc;

            Net.arcs.Add(newArc);

            from.ThisArcs.Add(newArc);
            if (from != to)
                to.ThisArcs.Add(newArc);

            DrawArc(newArc);

            if (!newArc.IsDirected) return newArc;

            var lineVisible = GetKeyByValueForArcs(newArc, DictionaryForArcs);
            DrawArrowHeads(lineVisible);
            GetKeyByValueForArcs(newArc, DictionaryForArrowHeads1).MouseDown += MouseArcDown;
            GetKeyByValueForArcs(newArc, DictionaryForArrowHeads2).MouseDown += MouseArcDown;
            return newArc;
        }

        private bool DoesArcAlreadyExist(VArc arc, IList<VArc> arcs)
        {
            var isExistInArcsList = false;
            if (arcs.Count == 0) return false;
            foreach (var tmpArc in arcs)
                if ((tmpArc.From == arc.From) && (tmpArc.To == arc.To))
                {
                    isExistInArcsList = true;
                    MessageBox.Show(ARCALREADYEXISTS);
                }

            return isExistInArcsList;
        }

        private void DrawArc(VArc newArc)
        {
            if (newArc.To == newArc.From)
            {
                var pthFigure = new PathFigure();
                double x = newArc.From.CoordX, y = newArc.From.CoordY;
                pthFigure.StartPoint = new Point(x + 15, y);

                var point1 = new Point(x - 10, y - 40);
                var point2 = new Point(x + 40, y - 40);
                var point3 = new Point(x + 15, y);

                var bzSeg = new BezierSegment
                {
                    Point1 = point1,
                    Point2 = point2,
                    Point3 = point3
                };

                var myPathSegmentCollection = new PathSegmentCollection {bzSeg};

                pthFigure.Segments = myPathSegmentCollection;

                var pthFigureCollection = new PathFigureCollection {pthFigure};

                var pthGeometry = new PathGeometry {Figures = pthFigureCollection};

                var arcPath = new System.Windows.Shapes.Path
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Data = pthGeometry
                };

                MainModelCanvas.Children.Add(arcPath);
                DictionaryForArcs.Add(arcPath, newArc);
                arcPath.MouseDown += MouseArcDown;
            }
            else
            {
                var lineVisible = new Line
                {
                    StrokeThickness = 2,
                    Stroke = Brushes.Black,
                    Visibility = Visibility.Visible
                };

                VisUtil.SetCoordinatesOfLine(lineVisible, newArc);


                MainModelCanvas.Children.Add(lineVisible);
                DictionaryForArcs.Add(lineVisible, newArc);
                lineVisible.MouseDown += MouseArcDown;
            }
        }

        private void Scroll(MouseEventArgs e)
        {
            if (e.GetPosition(GridMainField).Y > (GridMainField.ActualHeight - 60))
            {
                ScrollViewerForMainModelCanvas.ScrollToVerticalOffset(
                    ScrollViewerForMainModelCanvas.VerticalOffset + 0.3);
            }
            else if (e.GetPosition(GridMainField).Y < 60)
            {
                ScrollViewerForMainModelCanvas.ScrollToVerticalOffset(
                    ScrollViewerForMainModelCanvas.VerticalOffset - 0.3);
            }

            if (e.GetPosition(GridMainField).X > (GridMainField.ActualWidth - 60))
            {
                ScrollViewerForMainModelCanvas.ScrollToHorizontalOffset(
                    ScrollViewerForMainModelCanvas.HorizontalOffset + 0.3);
            }
            else if (e.GetPosition(GridMainField).X < 60)
            {
                ScrollViewerForMainModelCanvas.ScrollToHorizontalOffset(
                    ScrollViewerForMainModelCanvas.HorizontalOffset - 0.3);
            }
        }

        private void DisplayArc(VArc arc)
        {
            if (DictionaryForArcs.ContainsValue(arc))
                DictionaryForArcs.Remove(GetKeyByValueForArcs(arc, DictionaryForArcs));

            DrawArc(arc);
            RedrawArrowHeads(arc);
            AddWeightLabel(arc, arc.Weight);
            arc.AddToThisArcsLists();
        }

        private void RedrawArrowHeads(VArc arc)
        {
            if (!arc.IsDirected) return;

            if (GetKeyByValueForArcs(arc, DictionaryForArrowHeads1) != null)
            {
                MainModelCanvas.Children.Remove(GetKeyByValueForArcs(arc, DictionaryForArrowHeads1));
                MainModelCanvas.Children.Remove(GetKeyByValueForArcs(arc, DictionaryForArrowHeads2));

                DictionaryForArrowHeads1.Remove(GetKeyByValueForArcs(arc, DictionaryForArrowHeads1));
                DictionaryForArrowHeads2.Remove(GetKeyByValueForArcs(arc, DictionaryForArrowHeads2));
            }

            var arcShape = GetKeyByValueForArcs(arc, DictionaryForArcs);
            DrawArrowHeads(arcShape);

            GetKeyByValueForArcs(arc, DictionaryForArrowHeads1).MouseDown += MouseArcDown;
            GetKeyByValueForArcs(arc, DictionaryForArrowHeads2).MouseDown += MouseArcDown;
        }

        public void DrawArrowHeads(Shape arc)
        {
            var line = arc as Line;
            var arrow = line ?? VisUtil.GetLineFromPath(arc as Path);

            var arrowHead1 = new Line();
            var arrowHead2 = new Line();

            if (Math.Abs(arrow.X1 - arrow.X2) < 0.00001)
            {
                arrowHead1.X1 = arrow.X2 + 4;
                arrowHead2.X1 = arrow.X2 - 4;
                if (arrow.Y1 < arrow.Y2)
                    arrowHead1.Y1 = arrowHead2.Y1 = arrow.Y2 - 10;
                else
                    arrowHead1.Y1 = arrowHead2.Y1 = arrow.Y2 + 10;
            }
            else if (Math.Abs(arrow.Y1 - arrow.Y2) < 0.00001)
            {
                arrowHead1.Y1 = arrow.Y2 + 4;
                arrowHead2.Y1 = arrow.Y2 - 4;
                if (arrow.X1 < arrow.X2)
                    arrowHead1.X1 = arrowHead2.X1 = arrow.X2 - 10;
                else arrowHead1.X1 = arrowHead2.X1 = arrow.X2 + 10;
            }
            else
            {
                var xVector = arrow.X2 - arrow.X1;
                var yVector = arrow.Y2 - arrow.Y1;
                var vectorLength = Math.Sqrt(Math.Pow(xVector, 2) + Math.Pow(yVector, 2));
                var xUnitVector = xVector / vectorLength;
                var yUnitVector = yVector / vectorLength;
                var xTempPoint = arrow.X2 - 10 * xUnitVector;
                var yTempPoint = arrow.Y2 - 10 * yUnitVector;
                var xNormalUnitVector = yVector / vectorLength;
                var yNormalUnitVector = -xVector / yVector * xNormalUnitVector;
                var xNormalVector = 4 * xNormalUnitVector;
                var yNormalVector = 4 * yNormalUnitVector;
                arrowHead1.X1 = xTempPoint - xNormalVector;
                arrowHead1.Y1 = yTempPoint - yNormalVector;
                arrowHead2.X1 = xTempPoint + xNormalVector;
                arrowHead2.Y1 = yTempPoint + yNormalVector;
            }

            arrowHead1.X2 = arrow.X2;
            arrowHead1.Y2 = arrow.Y2;
            arrowHead2.X2 = arrow.X2;
            arrowHead2.Y2 = arrow.Y2;
            arrowHead1.StrokeThickness = 1.5;
            arrowHead1.Stroke = Brushes.Black;
            arrowHead1.Visibility = Visibility.Visible;
            arrowHead2.StrokeThickness = 1.5;
            arrowHead2.Stroke = Brushes.Black;
            arrowHead2.Visibility = Visibility.Visible;
            MainModelCanvas.Children.Add(arrowHead1);
            MainModelCanvas.Children.Add(arrowHead2);
            VArc tempArc;
            DictionaryForArcs.TryGetValue(arc, out tempArc);
            if (tempArc != null)
            {
                DictionaryForArrowHeads1.Add(arrowHead1, tempArc);
                DictionaryForArrowHeads2.Add(arrowHead2, tempArc);
            }
            else
            {
                _arrowHeads[0] = arrowHead1;
                _arrowHeads[1] = arrowHead2;
            }
        }

        private void SetNewCoordinatesOfArc(VArc arc, Shape shape)
        {
            if (shape is Line)
            {
                Line line = (Line) shape;
                double x1, y1, x2, y2;

                MathUtil.SearchPointOfIntersection(arc.From.CoordX + PLACEWIDTH / 2,
                    arc.From.CoordY + PLACEHEIGHT / 2,
                    arc.To.CoordX + PLACEWIDTH / 2,
                    arc.To.CoordY + PLACEHEIGHT / 2, out x1, out y1);
                MathUtil.SearchPointOfIntersection(arc.To.CoordX + PLACEWIDTH / 2,
                    arc.To.CoordY + PLACEHEIGHT / 2,
                    arc.From.CoordX + PLACEWIDTH / 2,
                    arc.From.CoordY + PLACEHEIGHT / 2, out x2, out y2);

                line.X1 = x1;
                line.X2 = x2;
                line.Y1 = y1;
                line.Y2 = y2;
            }
            else
            {
                var path = shape as Path;

                var geom = path.Data as PathGeometry;
                var segment = geom.Figures[0].Segments[0] as BezierSegment;

                var x = arc.From.CoordX;
                var y = arc.From.CoordY;

                geom.Figures[0].StartPoint = new Point(x + PLACEWIDTH / 2, y);

                Debug.Assert(segment != null, "segment != null");

                segment.Point1 = new Point(x - PLACEWIDTH / 3, y - PLACEHEIGHT - PLACEHEIGHT / 3);
                segment.Point2 = new Point(x + PLACEWIDTH + PLACEWIDTH / 3, y - PLACEHEIGHT - PLACEHEIGHT / 3);
                segment.Point3 = new Point(x + PLACEWIDTH / 2, y);
            }
        }

        bool hideLabels;

        public static bool IsWindowClosing = false;
        public static bool IsCancelPressed = false;

        private const double ScaleRate = 1.1;
        double _thisScale = 1;
        readonly ScaleTransform _scaleTransform = new ScaleTransform();

        public static bool isCtrlPressed;
        public static bool isSomethingChanged = false;

        private Grid GetGridByLine(Polyline line)
        {
            return (from ex in _syncRelationsDictionary where ex.Value.Equals(line) select ex.Key).FirstOrDefault();
        }

        bool alreadyDeleted;

        const int spaceBetweenCells = 50;

        private void AddWeightLabel(VArc arc, string content)
        {
            if (arc.Weight != "1")
            {
                Label weight = new Label();
                weight.Content = content;
                Canvas.SetLeft(weight, (arc.From.CoordX + arc.To.CoordX) / 2);
                Canvas.SetTop(weight, (arc.From.CoordY + arc.To.CoordY) / 2 - 5);
                MainModelCanvas.Children.Add(weight);
                arc.WeightLabel = weight;
            }
        }

        private void TurnOnSelectMode()
        {
            btnSelect.Focus();
            btnSelect.IsEnabled = false;
            _leftMouseButtonMode = LeftMouseButtonMode.Select;
        }

        private void SetCoordinatesByMesh(PetriNetNode selected)
        {
            if (selected is VPlace)
            {
                selected.Column = (int) ((selected.CoordX + PLACEWIDTH / 2) / PETRINETCELLWIDTH) + 1;
                selected.Row = (int) ((selected.CoordY + PLACEHEIGHT / 2) / PETRINETCELLHEIGHT) + 1;
                selected.CoordX = 10 + PETRINETCELLWIDTH * (selected.Column - 1);
                selected.CoordY = 20 + PETRINETCELLHEIGHT * (selected.Row - 1);
            }
            else
            {
                selected.Column = (int) ((selected.CoordX + TRANSITIONWIDTH / 2) / PETRINETCELLWIDTH) + 1;
                selected.Row = (int) ((selected.CoordY + TRANSITIONHEIGHT / 2) / PETRINETCELLHEIGHT) + 1;
                selected.CoordX = 10 + PETRINETCELLWIDTH * (selected.Column - 1);
                selected.CoordY = 10 + PETRINETCELLHEIGHT * (selected.Row - 1);
            }

            MoveFigure(selected);
        }

        //todo разделить работу с моделью и канвасом
        public void MoveFigure(PetriNetNode figure)
        {
            Canvas.SetLeft(GetKeyByValueForFigures(figure) as Shape, figure.CoordX);
            Canvas.SetTop(GetKeyByValueForFigures(figure) as Shape, figure.CoordY);

            foreach (var arc in figure.ThisArcs)
            {
                var line = GetKeyByValueForArcs(arc, DictionaryForArcs);


                if (line is Line)
                    VisUtil.SetCoordinatesOfLine(line as Line, arc);
                else
                    SetNewCoordinatesOfArc(arc, line);

                RedrawArrowHeads(arc);
                ColorArrow(arc);
                ChangeCoordinatedOfWeightLabel(arc);
            }

            SetLabel(figure);

            VPlace place = figure as VPlace;
            if (place == null) return;
            //todo вот такую хрень приходится делать, так как токены (каринки токенов) 
            // не вложены в эллипс позиции, они просто удаляются и добавляются. Причем, 
            // и в картинку, и в модель данных. Ужас
            RemoveTokens(place);
            AddTokens(place);
        }

        private void ColorArrow(VArc arc)
        {
            Shape line = GetKeyByValueForArcs(arc, DictionaryForArcs);
            Shape line1 = GetKeyByValueForArcs(arc, DictionaryForArrowHeads1);
            Shape line2 = GetKeyByValueForArcs(arc, DictionaryForArrowHeads2);

            if (arc.IsSelect)
            {
                line.Stroke = Brushes.Coral;
                if (arc.IsDirected)
                {
                    line1.Stroke = Brushes.Coral;
                    line2.Stroke = Brushes.Coral;
                }
            }
            else
            {
                line.Stroke = Brushes.Black;
                if (arc.IsDirected)
                {
                    line1.Stroke = Brushes.Black;
                    line2.Stroke = Brushes.Black;
                }
            }
        }

        public void SetLabel(PetriNetNode selected)
        {
            Label label;
            NodesToLabelsInCanvas.TryGetValue(selected, out label);
            if (label == null) return;
            int x, y;
            if (selected is VTransition)
            {
                x = 10;
                y = 45;
            }
            else
            {
                x = 15;
                y = 25;
            }

            double coord = selected.CoordX + x - label.Content.ToString().Length * 4;
            if (coord < 0) coord = 0;
            Canvas.SetLeft(label, coord);
            Canvas.SetTop(label, selected.CoordY + y);
        }

        private void ClearCommandStacks()
        {
            Command.ExecutedCommands.Clear();
            Command.CanceledCommands.Clear();
            btnUndo.IsEnabled = false;
            btnRedo.IsEnabled = false;
        }

        double borderX = spaceBetweenCells, borderY = spaceBetweenCells;


        public void VisualizePetriNet(VPetriNet net)
        {
            ClearCanvasWithoutLoss();

            foreach (var node in net.Nodes)
            {
                DrawFigure(node);
            }

            foreach (var arc in net.arcs)
            {
                DisplayArc(arc);
            }

            VisUtil.ResizeCanvas(Net.Nodes, MainControl, MainModelCanvas);
        }

        public void DrawFigure(PetriNetNode figure)
        {
            Shape temp;

            if (figure is VPlace)
            {
                temp = new Ellipse
                {
                    Width = 30,
                    Height = 30
                };
            }
            else
            {
                temp = new Rectangle
                {
                    Width = 20,
                    Height = 50
                };
            }

            temp.Fill = new SolidColorBrush(Colors.White) {Opacity = 0};
            temp.StrokeThickness = 2;
            temp.Stroke = Brushes.Black;
            temp.Visibility = Visibility.Visible;

            Canvas.SetLeft(temp, figure.CoordX);
            Canvas.SetTop(temp, figure.CoordY);

            if (temp is Ellipse)
            {
                temp.MouseDown += MousePlaceDown;
                temp.MouseUp += MousePlaceUp;
            }
            else
            {
                temp.MouseDown += MouseTransitionDown;
                temp.MouseUp += MouseTransitionUp;
            }

            MainModelCanvas.Children.Add(temp);

            _allFiguresObjectReferences.Add(temp, figure);

            NodesToLabelsInCanvas.Remove(figure);

            var label = new Label {Content = figure.Label};

            NodesToLabelsInCanvas.Add(figure, label);

            if (figure is VTransition)
            {
                Canvas.SetLeft(label, figure.CoordX + 10 - label.Content.ToString().Length * 4);
                Canvas.SetTop(label, figure.CoordY + 45);
            }
            else
            {
                Canvas.SetLeft(label, figure.CoordX + 15 - label.Content.ToString().Length * 4);
                Canvas.SetTop(label, figure.CoordY + 25);
            }

            MainModelCanvas.Children.Add(label);

            var place = figure as VPlace;
            if (place != null)
            {
                AddTokens(place);
            }

            if (_selectedFigures.Contains(figure))
                ((Shape) GetKeyByValueForFigures(figure)).Stroke = Brushes.Coral;
        }

        public void ClearCanvasWithoutLoss()
        {
            MainModelCanvas.Children.Clear();
            AddGridOnCanvas();

            HideOrShowGrid(_thisScale);

            _lineArcDrawing = new Line();
            MainModelCanvas.Children.Add(_lineArcDrawing);
            MainModelCanvas.Children.Add(_selectRect);
        }

        public void Activate()
        {
            SyncModel();
            //TODO: sync the model
            //throw new NotImplementedException();
        }

        public void UserControlKeyUp(object sender, KeyEventArgs e)
        {
            DisableRedoButton();
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                PNEditorControl.isCtrlPressed = false;
            }
        }

        private void SyncModel()
        {
            var clone = MainController.Self.Net.Clone();
            if (IsWindowClosing == false)
            {
                DeleteArcs(Net.arcs.ToList());
                while (Net.Nodes.Count != 0)
                    RemoveNode(Net.Nodes[0]);

                //Net.arcs.Clear();
                //SetOfFigures.Figures.Clear();
                ClearCanvasWithoutLoss();
                _selectedFigures.Clear();
                _selectedArcs.Clear();
                _selectedArc = null;
                _selectedFigure = null;
                MainController.Self.Net.Places.Clear();
                MainController.Self.Net.Places.AddRange(clone.Places);
                MainController.Self.Net.Transitions.Clear();
                MainController.Self.Net.Transitions.AddRange(clone.Transitions);
                MainController.Self.Net.Arcs.Clear();
                MainController.Self.Net.Arcs.AddRange(clone.Arcs);
                (var places, var transitions, var arcs) = ModelUtil.FromOriginalModel(MainController.Self.Net);
                Net.arcs.AddRange(arcs);
                Net.places.AddRange(places);
                Net.transitions.AddRange(transitions);
                //foreach (var figure in Net.Nodes)
                //{
                //    foreach (var arc in Net.arcs)
                //        if (arc.From == figure || arc.To == figure)
                //            figure.ThisArcs.Add(arc);
                //}
                foreach (PetriNetNode figure in Net.Nodes)
                {
                    DrawFigure(figure);
                }

                foreach (VArc arc in Net.arcs)
                {
                    DisplayArc(arc);
                }

                VisUtil.ResizeCanvas(Net.Nodes, MainControl, MainModelCanvas);
                btnUndo.IsEnabled = true;
                HideAllProperties();
                if (_thisScale != 1)
                {
                    _scaleTransform.ScaleX = 1;
                    _scaleTransform.ScaleY = 1;
                    _thisScale = 1;
                    MainModelCanvas.LayoutTransform = _scaleTransform;
                    MainModelCanvas.UpdateLayout();
                }

                btnGrid.IsEnabled = true;

                currentFileName = null;
                //TODO: do we need disable save or not
                menuSave.IsEnabled = false;
            }

            ShowMainWindowTitleDelegate("Carassius - Petri Net Editor");
        }
    }
}