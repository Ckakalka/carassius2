using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using GraphVisualizationModule;

namespace PNEditorSimulateView
{
    /// <summary>
    /// Author: Natalia Nikitina / Alexey Mitsyuk
    /// Simulation Interface and Functions
    /// PAIS Lab, 2014
    /// </summary>
    public partial class PNEditorControl
    {

        public List<VArc> GetOutgoingArcs(PetriNetNode figure)
        {
            return figure.ThisArcs.Where(t => t.From == figure).ToList();
        }

        public List<VArc> GetIngoingArcs(PetriNetNode figure)
        {
            return figure.ThisArcs.Where(t => t.To == figure).ToList();
        }

        #region SIMULATION-GUI

        enum SimulationMode { wave, simple };
        SimulationMode simulationMode = SimulationMode.simple;
        bool isWaveMode;

        private void btnOneStepSimulate_Click(object sender, RoutedEventArgs e)
        {
            btnOneStepSimulate.Focusable = false;
            if (Net.places.Count != 0)
            {
                btnReset.IsEnabled = true;
                isSimulatingEnds = false;
                btnOneStepBackSimulate.IsEnabled = true;
                isSimulatingEnds = SimulateOneStep();
                if (isSimulatingEnds)
                {
                    MessageBox.Show("The end");
                    if (marking.Count > 0)
                        marking.Pop();
                }
            }

            if (_leftMouseButtonMode == LeftMouseButtonMode.Select)
            {
                btnSelect.Focus();
                btnSelect.IsEnabled = false;
            }
        }

        private void btnSimulate_Click(object sender, RoutedEventArgs e)
        {
            btnSimulate.IsEnabled = false;
            btnSimulate.Focusable = false;
            btnStop.IsEnabled = true;
            //if (Place.places.Count != 0)
            if (Net.places.Count != 0)
            {
                isSimulatingEnds = false;
                timer = new System.Windows.Threading.DispatcherTimer();
                timer.Tick += DoSteps;
                timer.Interval = new TimeSpan(0, 0, 1);
                timer.Start();
            }
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            btnReset.Focusable = false;
            btnSimulate.IsEnabled = true;
            btnOneStepBackSimulate.IsEnabled = false;

            textBoxSimulationCurrentMarking.Clear();
            if (timer != null)
                timer.Stop();
            while (marking.Count > 1)
            {
                marking.Pop();
            }
            maxMarking = 0;

            oneStepMarking = new int[Net.places.Count];

            for (var i = 0; i < Net.places.Count; i++)
            {
                if (Net.places[i].NumberOfTokens != 0)
                {
                    if (Net.places[i].NumberOfTokens < 5)
                        RemoveTokens(Net.places[i]);
                    else
                        MainModelCanvas.Children.Remove(Net.places[i].NumberOfTokensLabel);
                }
                int weight;
                weights.TryGetValue(Net.places[i].Id, out weight);
                Net.places[i].NumberOfTokens = weight;
                oneStepMarking[i] = Net.places[i].NumberOfTokens;
                AddTokens(Net.places[i]);
            }
            marking.Push(oneStepMarking);
            maxMarking++;

            ResetColoring();

            textBoxSimulationCurrentMarking.Text += "M_0 = { ";
            for (var i = 0; i < oneStepMarking.Length - 1; i++)
            {
                textBoxSimulationCurrentMarking.Text += oneStepMarking[i] + " | ";
            }
            if(oneStepMarking.Length > 0)
            textBoxSimulationCurrentMarking.Text += oneStepMarking[oneStepMarking.Length - 1] + " }\n";

            btnReset.IsEnabled = false;
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if(btnSimulate.IsEnabled == false && btnStop.IsEnabled == true)
            {
                btnStop.Focusable = false;
                btnStop.IsEnabled = false;
                btnReset.IsEnabled = true;
                btnSimulate.IsEnabled = true;
                if(timer != null)
                {
                    timer.Stop();
                }                
                MessageBox.Show("Simuation is stopped");
                TurnOnSelectMode();
            }
        }

        private void btnOneStepBackSimulate_Click(object sender, RoutedEventArgs e)
        {
            var mark = new int[marking.Count][];
            marking.CopyTo(mark, 0);
            Array.Reverse(mark);

            textBoxSimulationCurrentMarking.Clear();

            for (var i = 0; i < mark.Length; i++)
            {
                if (i == mark.Length - 1 && maxMarking == marking.Count) continue;

                for (var j = 0; j < mark[i].Length; j++)
                {
                    textBoxSimulationCurrentMarking.Text += mark[i][j] + " ";
                }
                textBoxSimulationCurrentMarking.Text += "\n";
            }
            if (maxMarking < marking.Count - 1)
                textBoxSimulationCurrentMarking.Text += "\n";
            textBoxSimulationCurrentMarking.ScrollToEnd();

            btnOneStepBackSimulate.Focusable = false;

            if (maxMarking == marking.Count)
                marking.Pop();

            if (marking.Count == 1)
                btnOneStepBackSimulate.IsEnabled = false;

            maxMarking--;
            var lastMarking = marking.Count > 1 ? marking.Pop() : marking.Peek();

            btnSimulate.IsEnabled = true;
            for (var i = 0; i < Net.places.Count; i++)
            {
                if (Net.places[i].NumberOfTokens != 0)
                {
                    if (Net.places[i].NumberOfTokens < 5)
                        RemoveTokens(Net.places[i]);
                    else
                        MainModelCanvas.Children.Remove(Net.places[i].NumberOfTokensLabel);
                }
                Net.places[i].NumberOfTokens = lastMarking[i];
                AddTokens(Net.places[i]);
            }

            if (simulationMode == SimulationMode.wave)
            {

                //TODO What is here? : тут я пыталась не сбрасывать раскраску при отмене шага, а возращать в прежнее состояние
                /*if (colored[colored.Count - 1] is Transition)
                {
                    List<Arc> arcs = getIngoingArcs(colored[colored.Count - 1]);
                    for (int i = 0; i < arcs.Count; i++)
                    {
                        colored.Remove(arcs[i].From);
                    }
                    colored.RemoveAt(colored.Count - 1);
                }
                else
                {
                    int j = colored.Count - 2;
                   // if (j >= 0)
                    {
                        while ((colored[j] is Place))
                        {
                            j--;
                        }
                        List<Arc> arcs = getOutgoingArcs(colored[j]);
                        for (int i = 0; i < arcs.Count; i++)
                        {
                            colored.Remove(arcs[i].To);
                        }
                        colored.Remove(colored[j]);
                    }
                }
                numberOfLevels -= 2;
                if (colored.Count == 1)
                {
                    colored.Clear();
                    numberOfLevels--;
                }

                for (int i = 0; i < PetriNetNode.figures.Count; i++)
                {
                    (FiguresMethods.getKeyByValueForFigures(PetriNetNode.figures[i], FiguresMethods.myDictionary) as Shape).Fill =
                        Brushes.White;
                }*/
                ResetColoring();
            }
            TurnOnSelectMode();
        }

        private void btnSimulationMode_Click(object sender, RoutedEventArgs e)
        {
            if (Net.places.Count <= 0) return;
            marking.Clear();
            maxMarking = 0;

            lbllSimulationCurrentMarkingLegend.Content = "M_n = { ";
            for (int i = 0; i < Net.places.Count - 1; i++)
            {
                lbllSimulationCurrentMarkingLegend.Content += Net.places[i].Id + " | ";
            }
            //LabelMarkingLegend.Content += Place.places[Place.places.Count - 1].Id + " }";
            lbllSimulationCurrentMarkingLegend.Content += Net.places[Net.places.Count - 1].Id + " }";

            //oneStepMarking = new int[Place.places.Count];
            oneStepMarking = new int[Net.places.Count];
            //for (int i = 0; i < Place.places.Count; i++)
            for (int i = 0; i < Net.places.Count; i++)
            {
                //oneStepMarking[i] = Place.places[i].NumberOfTokens;
                oneStepMarking[i] = Net.places[i].NumberOfTokens;
            }
            marking.Push(oneStepMarking);
            maxMarking++;

            textBoxSimulationCurrentMarking.Text += "M_0 = { ";
            for (int i = 0; i < oneStepMarking.Length - 1; i++)
            {
                textBoxSimulationCurrentMarking.Text += oneStepMarking[i] + " | ";
            }
            textBoxSimulationCurrentMarking.Text += oneStepMarking[oneStepMarking.Length - 1] + " }\n";

            TurnOnSelectMode();
            isItFirstStep = true;


            lblName.Visibility = Visibility.Collapsed;
            tbName.Visibility = Visibility.Collapsed;
            btnOkName.Visibility = Visibility.Collapsed;



            ShowSimulationProperties();

            pnlLeftToolPanel.Visibility = Visibility.Collapsed;
            pnlSimulationBottom.Visibility = Visibility.Visible;
            pnlSimulationTools.Visibility = Visibility.Visible;
        }

        private void btnForcedChoice_Click(object sender, RoutedEventArgs e)
        {
            _modeChoice = Choice.Forced;
            btnRandomChoice.IsEnabled = true;
            btnforcedChoice.IsEnabled = false;
        }

        private void btnRandomChoice_Click(object sender, RoutedEventArgs e)
        {
            _modeChoice = Choice.Random;
            btnRandomChoice.IsEnabled = false;
            btnforcedChoice.IsEnabled = true;
        }

        private void btnWaveMode_Click(object sender, RoutedEventArgs e)
        {
            if (isWaveMode == false)
            {
                isWaveMode = true;
                simulationMode = SimulationMode.wave;
                btnWaveMode.Content = "Simple mode";
            }
            else
            {
                isWaveMode = false;
                simulationMode = SimulationMode.simple;
                btnWaveMode.Content = "Wave mode";
                ResetColoring();
            }

        }

        public void HideSimulationProperties()
        {
            btnOneStepSimulate.IsEnabled = false;
            btnSimulate.IsEnabled = false;
            btnStop.IsEnabled = false;
            btnReset.IsEnabled = false;
            btnOneStepBackSimulate.IsEnabled = false;

            textBoxSimulationCurrentMarking.IsEnabled = false;
            btnforcedChoice.IsEnabled = false;
            btnRandomChoice.IsEnabled = false;
            btnWaveMode.IsEnabled = false;
        }

        public void ShowSimulationProperties()
        {
            btnOneStepSimulate.IsEnabled = true;
            btnSimulate.IsEnabled = true;
            btnStop.IsEnabled = true;
            btnReset.IsEnabled = true;
            btnOneStepBackSimulate.IsEnabled = true;
            textBoxSimulationCurrentMarking.IsEnabled = true;
            btnforcedChoice.IsEnabled = true;
            btnRandomChoice.IsEnabled = false;
            btnWaveMode.IsEnabled = true;
        }

        public void ColorFigures()
        {
            var brightness = 1.0;
            var delta = brightness / numberOfLevels;
            MakeFiguresBlack(Net.Nodes, Net.arcs);
            foreach (var figure in Net.Nodes)
            {
                (GetKeyByValueForFigures(figure) as Shape).Fill.Opacity = 1.0;
                var brush = new SolidColorBrush { Color = Colors.White };
                (GetKeyByValueForFigures(figure) as Shape).Fill = brush;
            }

            var counter = 0;
            var quantity = 0;
            var reset = true;

            for (var i = colored.Count - 1; i >= 0; i--)
            {
                var temp = GetKeyByValueForFigures(colored[i]) as Shape;
                var brush = new SolidColorBrush(Colors.Blue) { Opacity = brightness };
                counter++;
                temp.Fill = brush;

                if (reset)
                {
                    if (colored[i] is VPlace)
                    {
                        var arcs = GetIngoingArcs(colored[i]);
                        if (arcs.Count != 0)
                            quantity = GetOutgoingArcs(arcs[0].From).Count;
                        else
                        {
                            var j = i + 1;
                            if (j < colored.Count)
                            {
                                while ((colored[j] is VPlace))
                                {
                                    j++;
                                }
                                quantity = GetIngoingArcs(colored[j]).Count;
                            }
                            else quantity = 1;
                        }
                    }
                    else quantity = 1;
                    reset = false;
                }

                if (counter != quantity) continue;

                counter = 0;
                brightness -= delta;
                reset = true;
            }
        }

        public void MakeFiguresBlack(List<PetriNetNode> figures, IList<VArc> arcs)
        {
            object temp;
            Line tempAsLine;
            foreach (PetriNetNode p in figures)
            {
                temp = GetKeyByValueForFigures(p);
                Shape tempAsShape = temp as Shape;
                if (tempAsShape != null) tempAsShape.Stroke = Brushes.Black;
            }
            foreach (VArc a in arcs)
            {
                temp = GetKeyByValueForArcs(a, DictionaryForArcs);
                tempAsLine = temp as Line;
                if (tempAsLine != null) tempAsLine.Stroke = Brushes.Black;
            }

            foreach (VArc a in arcs)
            {
                if (a.IsDirected)
                {
                    temp = GetKeyByValueForArcs(a, DictionaryForArrowHeads1);
                    tempAsLine = temp as Line;
                    if (tempAsLine != null) tempAsLine.Stroke = Brushes.Black;

                    temp = GetKeyByValueForArcs(a, DictionaryForArrowHeads2);
                    tempAsLine = temp as Line;
                    if (tempAsLine != null) tempAsLine.Stroke = Brushes.Black;
                }
            }
        }

        public void ResetColoring()
        {
            foreach (PetriNetNode figure in Net.Nodes)
            {
                SolidColorBrush brush = new SolidColorBrush(Colors.White);
                brush.Opacity = 1.0;
                (GetKeyByValueForFigures(figure) as Shape).Fill = brush;
            }
            colored.Clear();
            numberOfLevels = 0;
        }

        #endregion SIMULATION-GUI


        #region SIMULATION-FUNC

        public static Stack<int[]> marking = new Stack<int[]>();

        Dictionary<string, int> weights = new Dictionary<string, int>();

        bool isItFirstStep = true;
        int[] oneStepMarking;
        VTransition enabledTransition;
        List<PetriNetNode> colored = new List<PetriNetNode>();

        int numberOfLevels;
        int maxMarking;

        System.Windows.Threading.DispatcherTimer timer;
        bool isSimulatingEnds;


        public bool SimulateOneStep()
        {
            isSomethingChanged = true;

            if (isItFirstStep)
            {
                weights.Clear();

                foreach (var place in Net.places)
                {
                    weights.Add(place.Id, place.NumberOfTokens);
                }
                isItFirstStep = false;
            }

            // позиции, в которых есть токены
            List<VPlace> initialPlaces = new List<VPlace>();

            foreach (VPlace place in Net.places)
            {
                int numberOfOutgoingArcs = place.ThisArcs.Count(t => place != t.To);

                //todo вот здесь если токены не кружками, то будет плохо
                //if (place.TokensList.Count != 0 && numberOfOutgoingArcs != 0)
                if (place.NumberOfTokens != 0 && numberOfOutgoingArcs != 0)
                {
                    initialPlaces.Add(place);
                }
                foreach (Ellipse ellipse in place.TokensList)
                {
                    ellipse.Fill = Brushes.Black;
                }
            }

            // переходы, которые могут сработать
            var outgoingTransitions = new List<VTransition>();

            // проход по всем позициям, где есть токены
            foreach (var place in initialPlaces)
            {
                // проход по всем ребрам, смежным с этой позицией
                foreach (var arc1 in place.ThisArcs)
                {
                    // смотрим только на исходящие ребра
                    if (arc1.From != place) continue;

                    // проход по всем ребрам перехода, в который ведет текущее ребро
                    bool mayBeEnabled = false;
                    foreach (var arc2 in arc1.To.ThisArcs)
                    {
                        mayBeEnabled = true;
                        // смотрим только на входящие в текущий переход
                        if (arc2.To != arc1.To) continue;

                        int numberOfRequiredTokens;
                        int.TryParse(arc2.Weight, out numberOfRequiredTokens);
                        var numberOfExistingTokens = (arc2.From as VPlace).NumberOfTokens;

                        if (numberOfRequiredTokens > numberOfExistingTokens)
                        {
                            mayBeEnabled = false;
                            break;
                        }
                    }
                    if (!outgoingTransitions.Contains(arc1.To as VTransition) && mayBeEnabled)
                        outgoingTransitions.Add(arc1.To as VTransition);
                }
            }

            if (outgoingTransitions.Count != 0)
            {
                foreach (VTransition transition in outgoingTransitions)
                {
                    (GetKeyByValueForFigures(transition)
                        as Shape).Stroke = Brushes.Black;
                }


                if (_isTransitionSelected == false)
                {
                    if (_modeChoice == Choice.Forced)
                    {
                        if (outgoingTransitions.Count > 1)
                        {
                            _leftMouseButtonMode = LeftMouseButtonMode.ChooseTransition;
                            foreach (VTransition transition in outgoingTransitions)
                            {
                                SolidColorBrush brush = new SolidColorBrush();
                                brush.Color = Color.FromRgb(255, 0, 51);
                                (GetKeyByValueForFigures(transition) as Shape).Stroke = brush;
                            }
                            return false;
                        }
                        else
                            enabledTransition = outgoingTransitions[0];
                    }
                    else
                    {
                        var transitionsWithTopPriority = new List<VTransition>();
                        outgoingTransitions.Sort(new Comparison<VTransition>((VTransition a, VTransition b) => (a.Priority - b.Priority)));

                        var transitionWithTopPriority = outgoingTransitions.Find(new Predicate<VTransition>((VTransition t) => t.Priority > 0));

                        int topPriority = 0;
                        if (transitionWithTopPriority != null)
                        {
                            topPriority = transitionWithTopPriority.Priority;
                        }
                        
                        outgoingTransitions = outgoingTransitions.FindAll(new Predicate<VTransition>((VTransition a) => (a.Priority == topPriority || a.Priority == 0)));

                        int indexOfEnabledTransition = MainRandom.Next(0, outgoingTransitions.Count);
                        enabledTransition = outgoingTransitions[indexOfEnabledTransition];
                    }
                }
                _isTransitionSelected = false;
                _leftMouseButtonMode = LeftMouseButtonMode.Select;

                var isFirstRemove = true;

                foreach (var arc in enabledTransition.ThisArcs)
                {
                    if (arc.From == enabledTransition) continue;

                    if (colored.Contains(arc.From))
                    {
                        colored.Remove(arc.From);
                        if (isFirstRemove)
                            numberOfLevels--;
                        isFirstRemove = false;
                    }
                    colored.Add(arc.From);
                }
                numberOfLevels++;

                if (colored.Contains(enabledTransition))
                {
                    colored.Remove(enabledTransition);
                    numberOfLevels--;
                }
                colored.Add(enabledTransition);

                numberOfLevels++;
                isFirstRemove = true;
                foreach (VArc arc in enabledTransition.ThisArcs)
                {
                    VPlace changedPlace;
                    // входящее ребро
                    if (arc.From != enabledTransition)
                    {
                        changedPlace = (arc.From as VPlace);
                        if (changedPlace.NumberOfTokens != 0)
                        {
                            if (changedPlace.NumberOfTokens < 5)
                                RemoveTokens(changedPlace);
                            else
                                MainModelCanvas.Children.Remove(changedPlace.NumberOfTokensLabel);
                        }
                        int delta;
                        int.TryParse(arc.Weight, out delta);
                        changedPlace.NumberOfTokens -= delta;
                    }
                    // исходящее ребро
                    else
                    {
                        changedPlace = (arc.To as VPlace);
                        if (changedPlace.NumberOfTokens != 0)
                        {
                            if (changedPlace.NumberOfTokens < 5)
                                RemoveTokens(changedPlace);
                            else
                                MainModelCanvas.Children.Remove(changedPlace.NumberOfTokensLabel);
                        }
                        int delta;
                        int.TryParse(arc.Weight, out delta);
                        changedPlace.NumberOfTokens += delta;
                        if (colored.Contains(changedPlace))
                        {
                            colored.Remove(changedPlace);
                            if (isFirstRemove)
                                numberOfLevels--;
                            isFirstRemove = false;
                        }
                        colored.Add(changedPlace);
                    }



                    AddTokens(changedPlace);
                    if (arc.From != enabledTransition) continue;
                    var placeToColor = (arc.To as VPlace);
                    foreach (var ellepse in placeToColor.TokensList)
                    {
                        var brush = new SolidColorBrush { Color = Color.FromRgb(153, 255, 102) };
                        ellepse.Fill = brush;
                        ellepse.Stroke = Brushes.Black;
                    }
                }
                numberOfLevels++;

                if (simulationMode == SimulationMode.wave)
                    ColorFigures();


                if (marking.Count <= 0) return false;
                oneStepMarking = new int[Net.places.Count];
                for (int j = 0; j < Net.places.Count; j++)
                    oneStepMarking[j] = Net.places[j].NumberOfTokens;

                marking.Push(oneStepMarking);
                maxMarking++;

                textBoxSimulationCurrentMarking.Text += "M_" + (maxMarking - 1) + " = { ";
                for (int j = 0; j < oneStepMarking.Length - 1; j++)
                {
                    textBoxSimulationCurrentMarking.Text += oneStepMarking[j] + " | ";
                }
                textBoxSimulationCurrentMarking.Text += oneStepMarking[oneStepMarking.Length - 1] + " }\n";
                textBoxSimulationCurrentMarking.ScrollToEnd();
                return false;
            }
            else
            {
                return true;
            }
        }

        public void DoSteps(object sender, EventArgs e)
        {
            isSimulatingEnds = false;
            btnOneStepBackSimulate.IsEnabled = true;
            isSimulatingEnds = SimulateOneStep();

            if (!isSimulatingEnds) return;

            timer.Stop();
            btnStop.IsEnabled = false;
            MessageBox.Show("The end");
            marking.Pop();
            btnReset.IsEnabled = true;

            if (_leftMouseButtonMode != LeftMouseButtonMode.Select) return;
            btnSelect.Focus();
            btnSelect.IsEnabled = false;
        }

        #endregion SIMULATION-FUNC
    }
}
