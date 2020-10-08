using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Core;

namespace PNEditorEditView
{
    /// <summary>
    /// Author: Natalia Nikitina / Alexey Mitsyuk
    /// Undo - Redo
    /// PAIS Lab, 2014
    /// </summary>
    public partial class PNEditorControl
    {
        [MenuItemHandler("edit/undo",21)]
        public void UndoMenuItem_Click()
        {
            btnUndo_Click(null,null);
        }
        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;
            if (Command.ExecutedCommands.Count != 0)
            {
                Command lastCommand = Command.ExecutedCommands.Peek();
                if (lastCommand is AddFigureCommand)
                {
                    PetriNetNode thisFigure = (lastCommand as AddFigureCommand).newFigure;
                    EnableAddButtons();

                    RemoveNode(thisFigure);

                    if (_selectedFigures.Contains(thisFigure))
                    {
                        _selectedFigures.Remove(thisFigure);
                        _selectedFigure = null;
                    }
                }
                else if (lastCommand is AddArcCommand)
                {
                    VArc thisArc = (lastCommand as AddArcCommand).newArc;
                    EnableAddButtons();

                    List<VArc> arcs = new List<VArc>();
                    arcs.Add(thisArc);
                    DeleteArcs(arcs);

                    if (_selectedArcs.Contains(thisArc))
                    {
                        _selectedArcs.Remove(thisArc);
                        _selectedArc = null;
                    }
                }
                else if (lastCommand is AddTokensCommand)
                {
                    PetriNetNode figure = (lastCommand as AddTokensCommand).markedPlace;
                    VPlace place = (VPlace) figure;
                    place.NumberOfTokens = (lastCommand as AddTokensCommand).oldNumber;
                    

                    if (place.NumberOfTokens == 0)
                        RemoveTokens(place);
                    if (place.NumberOfTokens >= 0 && place.NumberOfTokens < 5)
                        MainModelCanvas.Children.Remove(place.NumberOfTokensLabel);

                    AddTokens(place);

                    int temp = (lastCommand as AddTokensCommand).oldNumber;
                    (lastCommand as AddTokensCommand).oldNumber = (lastCommand as AddTokensCommand).newNumber;
                    (lastCommand as AddTokensCommand).newNumber = temp;
                }
                else if (lastCommand is ChangeNameCommand)
                {
                    PetriNetNode namedFigure = (lastCommand as ChangeNameCommand).namedFigure;
                    namedFigure.Label = (lastCommand as ChangeNameCommand).oldName;

                    ChangeLabel(namedFigure, (lastCommand as ChangeNameCommand).oldName);

                    string temp = (lastCommand as ChangeNameCommand).oldName;
                    (lastCommand as ChangeNameCommand).oldName = (lastCommand as ChangeNameCommand).newName;
                    (lastCommand as ChangeNameCommand).newName = temp;
                }
                else if (lastCommand is DragCommand)
                {
                    foreach (PetriNetNode figure in Net.Nodes)
                    {
                        foreach (PetriNetNode f in (lastCommand as DragCommand).figuresBeforeDrag)
                            if (figure.Id == f.Id)
                            {
                                figure.CoordX = f.CoordX;
                                figure.CoordY = f.CoordY;
                            }
                    }

                    //List<PetriNetNode> newList = SetOfFigures.Figures;

                    foreach (PetriNetNode figure in Net.Nodes)//newlist
                    {
                        MoveFigure(figure);
                    }
                }
                else if (lastCommand is DeleteCommand)
                {
                    var restoredFigures = (lastCommand as DeleteCommand).deletedFigures;
                    var restoredArcs = (lastCommand as DeleteCommand).deletedArcs;

                    //SetOfFigures.Figures.AddRange(restoredFigures);
                    foreach (PetriNetNode node in restoredFigures)
                    {
                        if (node is VPlace)
                            Net.places.Add(node as VPlace);
                        else 
                            Net.transitions.Add(node as VTransition);
                    }
                    Net.arcs.AddRange(restoredArcs);

                    foreach (var figure in Net.Nodes)
                    {
                        if (restoredFigures.Contains(figure))
                        {
                            DrawFigure(figure);
                        }
                    }
                    foreach (var arc in Net.arcs)
                    {
                        if (restoredArcs.Contains(arc))
                        {
                            DisplayArc(arc);
                        }
                    }
                }
                else if (lastCommand is ChangeWeightCommand)
                {
                    VArc arc = (lastCommand as ChangeWeightCommand).arc;
                    MainModelCanvas.Children.Remove((lastCommand as ChangeWeightCommand).newWeightLabel);
                    arc.Weight = (lastCommand as ChangeWeightCommand).oldWeight;
                    arc.WeightLabel = (lastCommand as ChangeWeightCommand).oldWeightLabel;

                    
                    if (arc.Weight != "1")
                    {
                        (lastCommand as ChangeWeightCommand).oldWeightLabel.Content = arc.Weight;
                        Canvas.SetLeft((lastCommand as ChangeWeightCommand).oldWeightLabel,
                            (arc.From.CoordX + arc.To.CoordX) / 2);
                        Canvas.SetTop((lastCommand as ChangeWeightCommand).oldWeightLabel,
                            (arc.From.CoordY + arc.To.CoordY) / 2 - 5);
                        MainModelCanvas.Children.Add((lastCommand as ChangeWeightCommand).oldWeightLabel);
                    }
                }
                else if (lastCommand is CutCommand)
                {
                    List<PetriNetNode> cuttedFigures = (lastCommand as CutCommand).cuttedFigures;
                    List<PetriNetNode> old = (lastCommand as CutCommand).oldFigures;
                    List<VArc> cuttedArcs = (lastCommand as CutCommand).implicitlyCuttedArcs;

                    //SetOfFigures.Figures.AddRange(cuttedFigures);
                    Net.arcs.AddRange(cuttedArcs);
                    foreach (PetriNetNode figure in cuttedFigures)//SetOfFigures.Figures)
                    {
                        //if (cuttedFigures.Contains(figure))
                        {
                            DrawFigure(figure);
                            figure.IsSelect = true;
                            foreach (PetriNetNode oldFigure in old)
                            {
                                if (figure.Id == oldFigure.Id)
                                {
                                    figure.CoordX = oldFigure.CoordX;
                                    figure.CoordY = oldFigure.CoordY;
                                    Shape shape = GetKeyByValueForFigures(figure) as Shape;
                                    Canvas.SetLeft(shape, figure.CoordX);
                                    Canvas.SetTop(shape, figure.CoordY);
                                    break;
                                }
                            }
                            MakeSelected(figure);
                        }
                    }
                    foreach (VArc arc in Net.arcs)
                    {
                        if (cuttedArcs.Contains(arc))
                        {
                            DisplayArc(arc);
                            _selectedArcs.Add(arc);
                            arc.IsSelect = true;
                            ColorArrow(arc);
                        }
                    }
                }
                else if (lastCommand is PasteCommand)
                {
                    List<PetriNetNode> figures = (lastCommand as PasteCommand).pastedfigures;
                    List<VArc> arcs = (lastCommand as PasteCommand).pastedArcs;

                    cuttedOrCopiedArcs.Clear();
                    cuttedOrCopiedFigures.Clear();

                    cuttedOrCopiedFigures.AddRange(figures);
                    cuttedOrCopiedArcs.AddRange(arcs);

                    CutFigures(cuttedOrCopiedFigures, cuttedOrCopiedArcs);

                    _selectedFigures.Clear();
                    _selectedArcs.Clear();
                    ReassignSelectedProperties();
                }
                Command.ExecutedCommands.Pop();
                Command.CanceledCommands.Push(lastCommand);
            } 
            // команда undo для вставки ручки 

            ReassignSelectedProperties();
            EnableUndoRedoButtons();
            if (Command.ExecutedCommands.Count == 0)
                btnUndo.IsEnabled = false;
            TurnOnSelectMode();
        }
        [MenuItemHandler("edit/redo",22)]
        public void RedoMenuItem_Click()
        {
            btnRedo_Click(null,null);
        }

        private void btnRedo_Click(object sender, RoutedEventArgs e)
        {
            btnSelect.IsEnabled = true;
            if (Command.CanceledCommands.Count != 0)
            {
                Command lastCommand = Command.CanceledCommands.Pop();
                if (lastCommand is AddFigureCommand)
                {
                    PetriNetNode currentFigure = (lastCommand as AddFigureCommand).newFigure;
                    Shape shape = (lastCommand as AddFigureCommand).shape;

                    if (currentFigure.IsSelect)
                        currentFigure.IsSelect = false;
                    if ((lastCommand as AddFigureCommand).newFigure is VPlace)
                    {
                        //Place.places.Add(currentFigure as Place);
                        Net.places.Add(currentFigure as VPlace);
                    }
                    else
                    {
                        //Transition.transitions.Add(currentFigure as Transition);
                        Net.transitions.Add(currentFigure as VTransition);
                    }
                    //SetOfFigures.Figures.Add(currentFigure);

                    Canvas.SetLeft(shape, currentFigure.CoordX);
                    Canvas.SetTop(shape, currentFigure.CoordY);
                    MainModelCanvas.Children.Add(shape);

                    _allFiguresObjectReferences.Add(shape, currentFigure);

                    AddFigureCommand newCommand = new AddFigureCommand(currentFigure, shape);
                    Command.ExecutedCommands.Push(newCommand);
                    btnUndo.IsEnabled = true;
                }
                else if (lastCommand is AddArcCommand)
                {
                    VArc newArc = (lastCommand as AddArcCommand).newArc;
                    if (newArc.IsSelect)
                        newArc.IsSelect = false;
                    Net.arcs.Add(newArc);
                    newArc.AddToThisArcsLists();

                    DrawArc(newArc);
                    RedrawArrowHeads(newArc);

                    AddArcCommand newCommand = new AddArcCommand(newArc);
                    Command.ExecutedCommands.Push(newCommand);
                }
                else if (lastCommand is AddTokensCommand)
                {
                    PetriNetNode figure = (lastCommand as AddTokensCommand).markedPlace;

                    VPlace place = (VPlace) figure;
                    place.NumberOfTokens = (lastCommand as AddTokensCommand).oldNumber;

                    if (place.NumberOfTokens == 0)
                        RemoveTokens(place);
                    if (place.NumberOfTokens >= 0 && place.NumberOfTokens < 5)
                        MainModelCanvas.Children.Remove(place.NumberOfTokensLabel);
                    AddTokens(place);

                    int temp = (lastCommand as AddTokensCommand).oldNumber;
                    (lastCommand as AddTokensCommand).oldNumber = (lastCommand as AddTokensCommand).newNumber;
                    (lastCommand as AddTokensCommand).newNumber = temp;

                    AddTokensCommand newCommand = new AddTokensCommand(place, (lastCommand as AddTokensCommand).oldNumber,
                        (lastCommand as AddTokensCommand).newNumber);
                    Command.ExecutedCommands.Push(newCommand);
                }
                else if (lastCommand is ChangeNameCommand)
                {
                    PetriNetNode namedFigure = (lastCommand as ChangeNameCommand).namedFigure;
                    namedFigure.Label = (lastCommand as ChangeNameCommand).oldName;

                    ChangeLabel(namedFigure, (lastCommand as ChangeNameCommand).oldName);

                    string temp = (lastCommand as ChangeNameCommand).oldName;
                    (lastCommand as ChangeNameCommand).oldName = (lastCommand as ChangeNameCommand).newName;
                    (lastCommand as ChangeNameCommand).newName = temp;

                    ChangeNameCommand newCommand = new ChangeNameCommand(namedFigure, (lastCommand as ChangeNameCommand).oldName,
                            (lastCommand as ChangeNameCommand).newName);
                    Command.ExecutedCommands.Push(newCommand);
                }
                else if (lastCommand is DragCommand)
                {
                    foreach (PetriNetNode figure in Net.Nodes)
                    {
                        foreach (PetriNetNode afterDragFigure in (lastCommand as DragCommand).figuresAfterDrag)
                            if (figure.Id == afterDragFigure.Id)
                            {
                                figure.CoordX = afterDragFigure.CoordX;
                                figure.CoordY = afterDragFigure.CoordY;
                            }
                    }

                    //List<PetriNetNode> newList = SetOfFigures.Figures;

                    foreach (PetriNetNode figure in Net.Nodes)//newList)
                    {
                        MoveFigure(figure);
                    }

                    DragCommand newCommand = new DragCommand((lastCommand as DragCommand).figuresBeforeDrag,
                        (lastCommand as DragCommand).figuresAfterDrag);
                    Command.ExecutedCommands.Push(newCommand);
                }
                else if (lastCommand is DeleteCommand)
                {
                    List<PetriNetNode> figures = (lastCommand as DeleteCommand).deletedFigures;
                    List<VArc> arcs = (lastCommand as DeleteCommand).deletedArcs;
                    DeleteFigures(figures, arcs);
                    DeleteCommand newCommand = new DeleteCommand(figures, arcs);
                    Command.ExecutedCommands.Push(newCommand);
                }
                else if (lastCommand is ChangeWeightCommand)
                {
                    VArc arc = (lastCommand as ChangeWeightCommand).arc;
                    MainModelCanvas.Children.Remove((lastCommand as ChangeWeightCommand).oldWeightLabel);

                    arc.Weight = (lastCommand as ChangeWeightCommand).newWeight;
                    (lastCommand as ChangeWeightCommand).newWeightLabel.Content = arc.Weight;

                    Canvas.SetLeft((lastCommand as ChangeWeightCommand).newWeightLabel, (arc.From.CoordX + arc.To.CoordX) / 2);
                    Canvas.SetTop((lastCommand as ChangeWeightCommand).newWeightLabel, (arc.From.CoordY + arc.To.CoordY) / 2 - 5);
                    MainModelCanvas.Children.Add((lastCommand as ChangeWeightCommand).newWeightLabel);

                    arc.WeightLabel = (lastCommand as ChangeWeightCommand).newWeightLabel;

                    ChangeWeightCommand newCommand = new ChangeWeightCommand(arc, (lastCommand as ChangeWeightCommand).oldWeight,
                        arc.Weight, (lastCommand as ChangeWeightCommand).oldWeightLabel, arc.WeightLabel);
                    Command.ExecutedCommands.Push(newCommand);
                }
                else if (lastCommand is CutCommand)
                {
                    cuttedOrCopiedArcs.Clear();
                    cuttedOrCopiedFigures.Clear();

                    List<PetriNetNode> figures = (lastCommand as CutCommand).cuttedFigures;
                    List<PetriNetNode> old = (lastCommand as CutCommand).oldFigures;
                    List<VArc> expArcs = (lastCommand as CutCommand).explicitlyCuttedArcs;
                    List<VArc> implArcs = (lastCommand as CutCommand).implicitlyCuttedArcs;

                    cuttedOrCopiedFigures.AddRange(figures);
                    cuttedOrCopiedArcs.AddRange(expArcs);

                    List<PetriNetNode> cutF = new List<PetriNetNode>();
                    List<VArc> cutA = new List<VArc>();
                    cutF.AddRange(cuttedOrCopiedFigures);
                    cutA.AddRange(implArcs);

                    CutFigures(cutF, cutA);
                    _selectedFigures.Clear();
                    _selectedArcs.Clear();
                    ReassignSelectedProperties();

                    CutCommand newCommand = new CutCommand(figures, old, expArcs, implArcs);
                    Command.ExecutedCommands.Push(newCommand);
                }
                else if (lastCommand is PasteCommand)
                {
                    if (nothingToPaste == false)
                    {
                        List<PetriNetNode> figuresToPaste = (lastCommand as PasteCommand).pastedfigures;
                        List<VArc> arcsToPaste = (lastCommand as PasteCommand).pastedArcs;

                        double minX = figuresToPaste[0].CoordX, minY = figuresToPaste[0].CoordY;

                        foreach (PetriNetNode figure in figuresToPaste)
                        {
                            if (figure.CoordX < minX)
                                minX = figure.CoordX;
                            if (figure.CoordY < minY)
                                minY = figure.CoordY;
                        }

                        if (cutOrCopy == cutOrCopyMode.copy)
                        {
                            UnselectFigures();//(selectedFigures, selectedArcs);
                        }

                        foreach (PetriNetNode figure in figuresToPaste)
                        {
                            if (cutOrCopy == cutOrCopyMode.copy)
                                figure.DetectIdMatches(Net.Nodes);
                            figure.CoordX += -minX + ScrollViewerForMainModelCanvas.HorizontalOffset;
                            figure.CoordY += -minY + ScrollViewerForMainModelCanvas.VerticalOffset;
                        }
                        PasteCommand newCommand = new PasteCommand(figuresToPaste, arcsToPaste);
                        Command.ExecutedCommands.Push(newCommand);

                        if (cutOrCopy == cutOrCopyMode.cut)
                        {
                            arcsToPaste.RemoveAll(ShouldBeDeleted);
                        }

                        PasteFiguresAndArcs(figuresToPaste, arcsToPaste);
                    }
                }
            }

            EnableUndoRedoButtons();
            ReassignSelectedProperties();
            if (Command.CanceledCommands.Count == 0)
                btnRedo.IsEnabled = false;
            if (Command.ExecutedCommands.Count > 0)
                btnUndo.IsEnabled = true;
            TurnOnSelectMode();
        }

    }
}
