namespace IAC4;
//pretty bugs :3 i like bug is pretty and nice to me very pritty bug like uhmmm moth and ugmmhmmm isopod and uhmmm piders uhhh and uhmmmm cats :3
public partial class MainWindow : Window
{
    internal Grid activePanel;
    internal bool isListening;
    internal int _nextProfileIndex = 0;
    private string _currentlyEditing = string.Empty;
    private string previousActivationBindText = "";
    private string previousActionBindText = "";
    private Border? overlayBorder;
    private bool isActivationBind;
    internal static ClickBind Temp_ActivationBind;
    internal static ClickBind Temp_ActionBind;
    private const string SettingsFileName = "State.json";
    private KlikerSnapshot? _originalSnapshot;
    private bool _isAnimating = false;
    record KlikerSnapshot(string Name, ClickBind Activation, ClickBind Action, int HoldDuration, int Delay, int MaxDelay, ushort BurstCount, bool HoldMode, bool ToggleMode, bool BurstMode);

    public MainWindow()
    {
        InitializeComponent();
        LoadSettings();
        _ = timeBeginPeriod(1);
        activePanel = MainPanel;

        InnerBorder.SizeChanged += (s, e) =>
        {
            var rect = new Rect(0, 0, InnerBorder.ActualWidth, InnerBorder.ActualHeight);
            InnerBorder.Clip = new RectangleGeometry(rect, 8, 8);
        };
        Bind.PreviewKeyDown += SetKeyBinding;
        BindClick.PreviewKeyDown += SetKeyPressBinding;
        Bind.PreviewMouseDown += SetMouseBinding;
        BindClick.PreviewMouseDown += SetMouseClickBinding;
        LowLevelInputCheckBorder.BorderThickness = LowLevelInput ? new Thickness(2, 2, 4, 4) : new Thickness(1, 1, 8, 8);
        AllowUpdateCheckingCheckBorder.BorderThickness = AllowUpdates ? new Thickness(2, 2, 4, 4) : new Thickness(1, 1, 8, 8);
        UseCustomTitlesCheckBorder.BorderThickness = UseCustomTitles ? new Thickness(2, 2, 4, 4) : new Thickness(1, 1, 8, 8);

        Loaded += MainWindow_Loaded;
    }
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (UseCustomTitles)
        await GetTitleAsync();

        if (AllowUpdates)
        {
            var updateResult = await CheckForUpdates();

            if (updateResult == true)
            {
                updatebox.Visibility = Visibility.Visible;
                if (updatebox.Child is Label label)
                    label.Content = "THERES AN UPPDATEEEEEEE!11 WAOWWWW"; //pls?
            }
            else if (updateResult == null)
            {
                updatebox.Visibility = Visibility.Visible;
                if (updatebox.Child is Label label)
                {
                    label.Content = "FAILED TO CHECK FOR UPDATES"; //stinky behaviour
                    SendLogMessage("Failed to check for upperdates :pensive: it could be github or your internet connection or firewall ig");
                }
            }
        }
    }
    internal static void SaveSettings()
    {
        var snapshot = new
        {
            LowLevelInput,
            AllowUpdates,
            UseCustomTitles,
            Klikers = GetAllKlikers().ToDictionary(
                    k => k.ClickerName,
                    k => new
                    {
                        ActivationKey = k.ActivationBind.Key.HasValue ? k.ActivationBind.Key.Value.ToString() : null,
                        ActivationMouse = k.ActivationBind.Mouse.HasValue ? k.ActivationBind.Mouse.Value.ToString() : null,
                        ActionKey = k.ActionBind.Key.HasValue ? k.ActionBind.Key.Value.ToString() : null,
                        ActionMouse = k.ActionBind.Mouse.HasValue ? k.ActionBind.Mouse.Value.ToString() : null,
                        k.HoldDuration,
                        k.Delay,
                        k.MaxDelay,
                        k.BurstCount,
                        k.HoldMode,
                        k.ToggleMode,
                        k.BurstMode,
                        ShouldSpam = k.BurstMode || k.HoldMode
                    }
                )
        };

        JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };
        JsonSerializerOptions opts = jsonSerializerOptions;
        var json = JsonSerializer.Serialize(snapshot, opts);
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName), json);
        SendLogMessage($"State saved to {SettingsFileName}");
    }
    internal void LoadSettings()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
        if (!File.Exists(path))
        {
            SendLogMessage("no state file found(HAIIII thank for usuinng me or smth and uhhh if u arent new here uhmmm uhhhh) using defaults or wtv");
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;

            if (root.TryGetProperty("LowLevelInput", out var lowLevelProp))
            {
                try
                {
                    LowLevelInput = lowLevelProp.GetBoolean();
                    LowLevelInputCheckBorder.BorderThickness = LowLevelInput ? new Thickness(2, 2, 4, 4) : new Thickness(1, 1, 8, 8);
                    foreach (var clicker in GetAllKlikers().ToArray())
                        clicker.UpdateActionBind(clicker.ActionBind);
                }
                catch (Exception ex)
                {
                    SendLogMessage($"no work to read LowLevelInput: {ex.Message}"); //:supershock:
                }
            }

            if (root.TryGetProperty("AllowUpdates", out var updatecheckprop))
            {
                try
                {
                    AllowUpdates = updatecheckprop.GetBoolean();
                    AllowUpdateCheckingCheckBorder.BorderThickness = AllowUpdates ? new Thickness(2, 2, 4, 4) : new Thickness(1, 1, 8, 8);
                }
                catch (Exception ex)
                {
                    SendLogMessage($"couldnt check what uhh 1 sec uhhhhhh right what the allow update box was: {ex.Message}"); //:supershock:
                }
            }

            if (root.TryGetProperty("UseCustomTitles", out var customtitleprop))
            {
                try
                {
                    UseCustomTitles = customtitleprop.GetBoolean();
                    UseCustomTitlesCheckBorder.BorderThickness = UseCustomTitles ? new Thickness(2, 2, 4, 4) : new Thickness(1, 1, 8, 8);
                }
                catch (Exception ex)
                {
                    SendLogMessage($"couldnt understand what value the use custom titles checkbox was: {ex.Message}"); //:supershock:
                }
            }

            ClearAll();
            ProfilesPanel.Children.Clear();
            _nextProfileIndex = 0;

            foreach (var prop in root.GetProperty("Klikers").EnumerateObject())
            {
                var name = prop.Name;
                var data = prop.Value;

                try
                {
                    string? actKeyStr = data.GetProperty("ActivationKey").GetString();
                    string? actMouseStr = data.GetProperty("ActivationMouse").GetString();
                    string? act2KeyStr = data.GetProperty("ActionKey").GetString();
                    string? act2MouseStr = data.GetProperty("ActionMouse").GetString();

                    ClickBind activation = actKeyStr is not null
                        ? new ClickBind { Key = Enum.Parse<Key>(actKeyStr) }
                        : actMouseStr is not null
                        ? new ClickBind { Mouse = Enum.Parse<MouseButton>(actMouseStr) }
                        : throw new InvalidOperationException("Neither ActivationKey nor ActivationMouse present.");

                    ClickBind action = act2KeyStr is not null
                        ? new ClickBind { Key = Enum.Parse<Key>(act2KeyStr) }
                        : act2MouseStr is not null
                        ? new ClickBind { Mouse = Enum.Parse<MouseButton>(act2MouseStr) }
                        : throw new InvalidOperationException("Neither ActionKey nor ActionMouse present.");

                    var hold = (ushort)data.GetProperty("HoldDuration").GetUInt32();
                    var delay = (ushort)data.GetProperty("Delay").GetUInt32();
                    var maxDelay = (ushort)data.GetProperty("MaxDelay").GetUInt32();
                    var burstCount = (ushort)data.GetProperty("BurstCount").GetUInt32();
                    var holdMode = data.GetProperty("HoldMode").GetBoolean();
                    var toggleMode = data.GetProperty("ToggleMode").GetBoolean();
                    var burstMode = data.GetProperty("BurstMode").GetBoolean();
                    var shouldSpam = data.GetProperty("ShouldSpam").GetBoolean();

                    LoadProfileUI(name, activation, action, hold, delay, maxDelay, burstCount, holdMode, toggleMode, burstMode, shouldSpam);
                }
                catch (Exception ex)
                {
                    SendLogMessage($"skipping '{name}': {ex.Message}");
                }
            }

            SendLogMessage($"Loaded state from {SettingsFileName}");
        }
        catch (Exception ex)
        {
            SendLogMessage($"not able to not not load state: {ex.Message}");
        }
    }
    internal void ResetSettings(object sender, RoutedEventArgs e)
    {
        ResetPressed = true;
        _ = ButtonAnimator();
        LowLevelInput = false;
        AllowUpdates = false;
        UseCustomTitles = false;
        LowLevelInputCheckBorder.BorderThickness = new Thickness(1, 1, 8, 8);
        AllowUpdateCheckingCheckBorder.BorderThickness = new Thickness(1, 1, 8, 8);
        UseCustomTitlesCheckBorder.BorderThickness = new Thickness(1, 1, 8, 8);
        UpdateProfiles();

        SendLogMessage("settings have been reset to default values yayyyg");
    }
    internal void ToSettingsPanel(object sender, RoutedEventArgs e)
    {
        if (!TransitionDone) return;
        if (activePanel == SettingsPanel) return;
        SlidePanel(activePanel, SettingsPanel, -1, false);
    }
    internal void ToProfilePanel(object sender, RoutedEventArgs e)
    {
        if (!TransitionDone) return;
        if (activePanel == ProfilePanel) return;
        SlidePanel(activePanel, ProfilePanel, 1, false);
    }
    internal void ToKlikerPanelY(object sender, RoutedEventArgs e)
    {
        if (!TransitionDone) return;
        if (activePanel == KlikerPanel) return;
        if (HasUnsavedChanges())
        {
            UnsavedChangesBorder.Visibility = Visibility.Visible;
            return;
        }
        SlidePanel(activePanel, KlikerPanel, -1, false);
    }
    internal void ToLogPanelY(object sender, RoutedEventArgs e)
    {
        if (!TransitionDone) return;
        if (activePanel == LogPanel) return;
        SlidePanel(activePanel, LogPanel, 1, false);
    }
    internal void ToKlikerPanel(object sender, RoutedEventArgs e)
    {
        if (!TransitionDone) return;
        if (activePanel == KlikerPanel) return;
        SlidePanel(activePanel, KlikerPanel, 1, true);
    }
    internal void ToLogPanel(object sender, RoutedEventArgs e)
    {
        if (!TransitionDone) return;
        if (activePanel == LogPanel) return;
        SlidePanel(activePanel, LogPanel, -1, true);
    }
    internal void ToMainPanel(object sender, RoutedEventArgs e)
    {
        if (!TransitionDone) return;
        if (activePanel == MainPanel) return;
        var dir = activePanel == KlikerPanel ? -1 : 1;
        SlidePanel(activePanel, MainPanel, dir, true);
    }
    internal void SlidePanelX(Grid from, Grid to, double direction)
    {
        SlidePanel(from, to, direction, true);
    }
    internal void SlidePanelY(Grid from, Grid to, double direction)
    {
        SlidePanel(from, to, direction, false);
    }
    internal void SlidePanel(Grid from, Grid to, double direction, bool isHorizontal)
    {
        TransitionDone = false;
        double offset = isHorizontal ? LayoutRoot.ActualWidth : LayoutRoot.ActualHeight;
        string property = isHorizontal ? "X" : "Y";
        to.Visibility = Visibility.Visible;
        var ttTo = (TranslateTransform)to.RenderTransform;
        if (isHorizontal)
            ttTo.X = direction * offset;
        else
            ttTo.Y = direction * offset;
        var duration = new Duration(TimeSpan.FromMilliseconds(500));
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var sb = new Storyboard();
        var animOut = new DoubleAnimation(0, -direction * offset, duration) { EasingFunction = easing };
        Storyboard.SetTarget(animOut, from);
        Storyboard.SetTargetProperty(animOut,
            new PropertyPath($"(UIElement.RenderTransform).(TranslateTransform.{property})"));
        sb.Children.Add(animOut);
        var animIn = new DoubleAnimation(direction * offset, 0, duration) { EasingFunction = easing };
        Storyboard.SetTarget(animIn, to);
        Storyboard.SetTargetProperty(animIn,
            new PropertyPath($"(UIElement.RenderTransform).(TranslateTransform.{property})"));
        sb.Children.Add(animIn);

        sb.Completed += (s, ev) =>
        {
            from.Visibility = Visibility.Collapsed;
            var ttFrom = (TranslateTransform)from.RenderTransform;
            ttTo = (TranslateTransform)to.RenderTransform;

            if (isHorizontal)
            {
                ttFrom.X = 0;
                ttTo.X = 0;
            }
            else
            {
                ttFrom.Y = 0;
                ttTo.Y = 0;
            }

            activePanel = to;
            TransitionDone = true;
        };
        sb.Begin();
    }
    private void LogOutput(object sender, TextChangedEventArgs e)
    {
    }
    private void SetMouseBinding(object sender, MouseButtonEventArgs e)
    {
        if (!isListening)
        {
            previousActivationBindText = Bind.Text;
            isActivationBind = true;
            StartListening("activation");
            e.Handled = true;
        }
    }
    private void SetKeyBinding(object sender, KeyEventArgs e)
    {
    }
    private void SetMouseClickBinding(object sender, MouseButtonEventArgs e)
    {
        if (!isListening)
        {
            previousActionBindText = BindClick.Text;
            isActivationBind = false;
            StartListening("action");
            e.Handled = true;
        }
    }
    private void SetKeyPressBinding(object sender, KeyEventArgs e)
    {
    }
    private void StartListening(string bindType)
    {
        isListening = true;
        var window = GetWindow(this);

        if (window.Content is Panel rootGrid)
        {
            overlayBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                Child = new TextBlock
                {
                    Text = $"Binding {bindType}...\nPress any key or mouse button\n(Press ESC to cancel)",
                    FontSize = 24,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                }
            };

            Grid.SetRowSpan(overlayBorder, 100);
            Grid.SetColumnSpan(overlayBorder, 100);
            Panel.SetZIndex(overlayBorder, 9999);
            rootGrid.Children.Add(overlayBorder);

            overlayBorder.PreviewKeyDown += OverlayKeyHandler;
            overlayBorder.PreviewMouseDown += OverlayMouseHandler;
            overlayBorder.Focusable = true;
            overlayBorder.Focus();
        }
    }
    private void OverlayKeyHandler(object sender, KeyEventArgs e)
    {
        if (!isListening) return;
        var key = (e.Key == Key.System) ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            CancelListening();
            e.Handled = true;
            return;
        }
        var bind = new ClickBind { Key = key };
        if (isActivationBind)
        {
            Temp_ActivationBind = bind;
            Bind.Text = key.ToString();
            previousActivationBindText = key.ToString();
        }
        else
        {
            Temp_ActionBind = bind;
            BindClick.Text = key.ToString();
            previousActionBindText = key.ToString();
        }
        StopListening();
        e.Handled = true;
    }
    private void OverlayMouseHandler(object sender, MouseButtonEventArgs e)
    {
        if (!isListening) return;
        var bind = new ClickBind { Mouse = e.ChangedButton };
        if (isActivationBind)
        {
            Temp_ActivationBind = bind;
            Bind.Text = e.ChangedButton.ToString();
            previousActivationBindText = e.ChangedButton.ToString();
        }
        else
        {
            Temp_ActionBind = bind;
            BindClick.Text = e.ChangedButton.ToString();
            previousActionBindText = e.ChangedButton.ToString();
        }
        StopListening();
        e.Handled = true;
    }
    private void CancelListening()
    {
        if (isActivationBind)
            Bind.Text = previousActivationBindText;
        else
            BindClick.Text = previousActionBindText;

        StopListening();
    }
    private void StopListening()
    {
        isListening = false;

        if (overlayBorder != null)
        {
            var window = GetWindow(this);

            if (window?.Content is Panel rootGrid && rootGrid.Children.Contains(overlayBorder))
            {
                overlayBorder.PreviewKeyDown -= OverlayKeyHandler;
                overlayBorder.PreviewMouseDown -= OverlayMouseHandler;
                rootGrid.Children.Remove(overlayBorder);
            }

            overlayBorder = null;
        }
    }
    private void Bind_LostFocus(object sender, RoutedEventArgs e)
    {
        if (isListening && overlayBorder == null && isActivationBind)
        {
            CancelListening();
        }
    }
    private void BindClick_LostFocus(object sender, RoutedEventArgs e)
    {
        if (isListening && overlayBorder == null && !isActivationBind)
        {
            CancelListening();
        }
    }
    private void MaxDelayTextBox(object sender, TextCompositionEventArgs e) => e.Handled = !int.TryParse(e.Text, out _);
    private void DelayTextBox(object sender, TextCompositionEventArgs e) => e.Handled = !int.TryParse(e.Text, out _);
    private void HoldDurTextBox(object sender, TextCompositionEventArgs e) => e.Handled = !int.TryParse(e.Text, out _);
    private void BurstCountTextBox(object sender, TextCompositionEventArgs e) => e.Handled = !int.TryParse(e.Text, out _);
    private void ProfileNameTextBox(object sender, TextCompositionEventArgs e) { }
    private void SaveProfileChanges(object sender, RoutedEventArgs e)
    {
        SaveChangesPressed = true;
        _ = ButtonAnimator();
        if (string.IsNullOrEmpty(_currentlyEditing))
            return;
        var newName = NProfileNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }
        bool nameClash = ProfilesPanel.Children.OfType<Button>().Select(b => b.Content?.ToString()).Any(content => content != null && content.Equals(newName, StringComparison.OrdinalIgnoreCase) && !content.Equals(_currentlyEditing, StringComparison.OrdinalIgnoreCase));

        if (nameClash)
        {
            return;
        }

        ClickBind activation = Temp_ActivationBind;
        ClickBind action = Temp_ActionBind;

        if (!(ushort.TryParse(NHoldDurTextBox.Text, out var holdDur) &&
              ushort.TryParse(NdelayTextBox.Text, out var delay) &&
              ushort.TryParse(NMaxDelayTextBox.Text, out var maxDelay)))
        {
            return;
        }

        if (!ushort.TryParse(NBurstCountTextBox.Text, out var burstCount))
        {
            return;
        }

        var holdMode = HTC.IsChecked == true;
        var toggleMode = TTC.IsChecked == true;
        var burstMode = PTB.IsChecked == true;

        if (!newName.Equals(_currentlyEditing, StringComparison.OrdinalIgnoreCase))
        {
            var btn = ProfilesPanel.Children
                .OfType<Panel>()
                .SelectMany(p => p.Children.OfType<Button>())
                .FirstOrDefault(b =>
                    string.Equals(b.Content?.ToString(), _currentlyEditing, StringComparison.OrdinalIgnoreCase));
            DeleteKliker(_currentlyEditing);

            CreateKliker(
                newName,
                activation,
                action,
                holdDur,
                delay,
                maxDelay,
                burstCount,
                holdMode,
                toggleMode,
                burstMode,
                shouldSpam: false
            );
            if (btn != null)
            {
                btn.Content = newName;
            }
            else
            {
                SendLogMessage($"UI button for '{_currentlyEditing}' not found during rename.");
            }

            _currentlyEditing = newName;
        }
        else
        {
            bool ok = UpdateKliker(
                newName,
                activation,
                action,
                holdDur,
                delay,
                maxDelay,
                burstCount,
                holdMode,
                toggleMode,
                burstMode
            );
            if (!ok)
            {
                SendLogMessage($"Failed to update profile '{newName}'.");
                return;
            }
        }
        var existing = GetKliker(_currentlyEditing);
        DebugNameLabel.Content = $"Name: {newName}";
        if (existing != null)
        {
            DebugActivationLabel.Content = existing.ActivationBind.IsKey ? existing.ActivationBind.Key.ToString() : existing.ActivationBind.Mouse.ToString();
            DebugActionLabel.Content = existing.ActionBind.IsKey ? existing.ActionBind.Key.ToString() : existing.ActionBind.Mouse.ToString();
        }
        DebugHoldDurLabel.Content = $"HoldDuration: {holdDur}";
        DebugDelayLabel.Content = $"Delay: {delay}";
        DebugMaxDelayLabel.Content = $"MaxDelay: {maxDelay}";
        DebugBurstCountLabel.Content = $"BurstCount: {burstCount}";
        DebugHoldModeLabel.Content = $"HoldMode: {holdMode}";
        DebugToggleModeLabel.Content = $"ToggleMode: {toggleMode}";
        DebugBurstModeLabel.Content = $"BurstMode: {burstMode}";
        DebugThreadLabel.Content = $"Profile is on CPU Thread #{existing?.ThreadId.ToString()}";
        if (existing != null)
            _originalSnapshot = new KlikerSnapshot(existing.ClickerName, existing.ActivationBind, existing.ActionBind, existing.HoldDuration, existing.Delay, existing.MaxDelay, existing.BurstCount, existing.HoldMode, existing.ToggleMode, existing.BurstMode);
        UpdateProfiles();
    }
    private void PTBChecked(object sender, RoutedEventArgs e)
    {
        if (PTB.IsChecked == null) return;

        BurstModeCheckBorder.BorderThickness = (bool)PTB.IsChecked ? new Thickness(1, 1, 2, 2) : new Thickness(1, 1, 4, 4);

        TTC.IsChecked = false;
        ToggleModeCheckBorder.BorderThickness = new Thickness(1, 1, 4, 4);
        HTC.IsChecked = false;
        HoldModeCheckBorder.BorderThickness = new Thickness(1, 1, 4, 4);
    }
    private void TTCChecked(object sender, RoutedEventArgs e)
    {
        if (TTC.IsChecked == null) return;

        ToggleModeCheckBorder.BorderThickness = (bool)TTC.IsChecked ? new Thickness(1, 1, 2, 2) : new Thickness(1, 1, 4, 4);

        PTB.IsChecked = false;
        BurstModeCheckBorder.BorderThickness = new Thickness(1, 1, 4, 4);
        HTC.IsChecked = false;
        HoldModeCheckBorder.BorderThickness = new Thickness(1, 1, 4, 4);
    }
    private void HTCChecked(object sender, RoutedEventArgs e)
    {
        if (HTC.IsChecked == null) return;

        HoldModeCheckBorder.BorderThickness = (bool)HTC.IsChecked ? new Thickness(1, 1, 2, 2) : new Thickness(1, 1, 4, 4);

        TTC.IsChecked = false;
        ToggleModeCheckBorder.BorderThickness = new Thickness(1, 1, 4, 4);
        PTB.IsChecked = false;
        BurstModeCheckBorder.BorderThickness = new Thickness(1, 1, 4, 4);
    }
    private void ProfileNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var name = ProfileNameTextBox2.Text.Trim();

        bool hasText = !string.IsNullOrWhiteSpace(name);
        bool alreadyExists = ProfilesPanel.Children.OfType<Button>().Any(b => b.Content is string content && content.Equals(name, StringComparison.OrdinalIgnoreCase));

        AddProfileButton.IsEnabled = hasText && !alreadyExists;

        TextNull = !hasText;
        NameExists = alreadyExists;

        ProfileNameTextBox2.ClearValue(ToolTipProperty);
    }
    private void AddProfile(object sender, RoutedEventArgs e)
    {
        AddProfilePressed = true;
        _ = ButtonAnimator();
        var name = ProfileNameTextBox2.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        bool alreadyExists =
            ProfilesPanel.Children.OfType<Button>().Any(b => string.Equals(b.Content?.ToString(), name, StringComparison.OrdinalIgnoreCase))
            || ProfilesPanel.Children.OfType<Grid>().SelectMany(g => g.Children.OfType<Button>()).Any(b => string.Equals(b.Content?.ToString(), name, StringComparison.OrdinalIgnoreCase))
            || ProfilesPanel.Children.OfType<StackPanel>().SelectMany(sp => sp.Children.OfType<Button>()).Any(b => string.Equals(b.Content?.ToString(), name, StringComparison.OrdinalIgnoreCase));

        if (alreadyExists) return;

        var grid = new Grid
        {
            Margin = new Thickness(0, 0, 0, 5),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var delBtn = new Button
        {
            Content = "X",
            Width = 30,
            Height = 40,
            Margin = new Thickness(0, 0, 0, 0),
            Cursor = Cursors.Hand,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(238, 46, 42)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(50, 46, 49)),
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            BorderThickness = new Thickness(1,1,1,4),
            Padding = new Thickness(0)
        };
        Grid.SetColumn(delBtn, 0);
        var delTemplate = new ControlTemplate(typeof(Button));
        var delBorderFactory = new FrameworkElementFactory(typeof(Border));
        delBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5, 0, 0, 5));
        delBorderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
        delBorderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(BorderBrushProperty));
        delBorderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(BorderThicknessProperty));
        var delContentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        delContentFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        delContentFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        delBorderFactory.AppendChild(delContentFactory);
        delTemplate.VisualTree = delBorderFactory;
        delBtn.Template = delTemplate;

        var btn = new Button
        {
            Content = name,
            Cursor = Cursors.Hand,
            Width = double.NaN,
            Height = 40,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 16,
            Opacity = 1,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(198, 195, 171)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(50, 46, 49)),
            Foreground = Brushes.Black,
            BorderThickness = new Thickness(0,1,4,4),
            Padding = new Thickness(0)
        };
        Grid.SetColumn(btn, 1);

        var profileTemplate = new ControlTemplate(typeof(Button));
        var profBorderFactory = new FrameworkElementFactory(typeof(Border));
        profBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(0, 5, 5, 0));
        profBorderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
        profBorderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(BorderBrushProperty));
        profBorderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(BorderThicknessProperty));
        var profContentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        profContentFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        profContentFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        profBorderFactory.AppendChild(profContentFactory);
        profileTemplate.VisualTree = profBorderFactory;
        btn.Template = profileTemplate;

        btn.Click += ProfileButton;

        delBtn.Click += (s, args) =>
        {
            DeleteKliker(name);
            ProfilesPanel.Children.Remove(grid);
        };

        grid.Children.Add(delBtn);
        grid.Children.Add(btn);
        ProfilesPanel.Children.Add(grid);

        var defaultActivation = new ClickBind { Key = Key.F1 };
        var defaultAction = new ClickBind { Mouse = MouseButton.Left };

        CreateKliker(
            name,
            defaultActivation,
            defaultAction,
            holdDuration: 0,
            delay: 0,
            maxDelay: 0,
            burstCount: 0,
            holdMode: true,
            toggleMode: false,
            burstMode: false,
            shouldSpam: true
        );

        ProfileNameTextBox2.Text = "";
        UpdateProfiles();
    }
    private void LoadProfileUI(string name, ClickBind activation, ClickBind action, ushort holdDuration, ushort delay, ushort maxDelay, ushort burstCount, bool holdMode, bool toggleMode, bool burstMode, bool shouldSpam)
    {
        var grid = new Grid
        {
            Margin = new Thickness(0, 0, 0, 5),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var delBtn = new Button
        {
            Content = "X",
            Width = 30,
            Height = 40,
            Margin = new Thickness(0, 0, 0, 0),
            Cursor = Cursors.Hand,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(238, 46, 42)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(50, 46, 49)),
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            BorderThickness = new Thickness(1, 1, 1, 4),
            Padding = new Thickness(0)
        };
        Grid.SetColumn(delBtn, 0);

        var delTemplate = new ControlTemplate(typeof(Button));
        var delBorderFactory = new FrameworkElementFactory(typeof(Border));
        delBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5, 0, 0, 5));
        delBorderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
        delBorderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(BorderBrushProperty));
        delBorderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(BorderThicknessProperty));
        var delContentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        delContentFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        delContentFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        delBorderFactory.AppendChild(delContentFactory);
        delTemplate.VisualTree = delBorderFactory;
        delBtn.Template = delTemplate;

        var btn = new Button
        {
            Content = name,
            Cursor = Cursors.Hand,
            Width = double.NaN,
            Height = 40,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 16,
            Opacity = 1,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(198, 195, 171)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(50, 46, 49)),
            Foreground = Brushes.Black,
            BorderThickness = new Thickness(0, 1, 4, 4),
            Padding = new Thickness(0)
        };
        Grid.SetColumn(btn, 1);

        var profileTemplate = new ControlTemplate(typeof(Button));
        var profBorderFactory = new FrameworkElementFactory(typeof(Border));
        profBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(0, 5, 5, 0));
        profBorderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
        profBorderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(BorderBrushProperty));
        profBorderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(BorderThicknessProperty));
        var profContentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        profContentFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        profContentFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        profBorderFactory.AppendChild(profContentFactory);
        profileTemplate.VisualTree = profBorderFactory;
        btn.Template = profileTemplate;

        btn.Click += ProfileButton;

        delBtn.Click += (s, args) =>
        {
            DeleteKliker(name);
            ProfilesPanel.Children.Remove(grid);
        };

        grid.Children.Add(delBtn);
        grid.Children.Add(btn);
        ProfilesPanel.Children.Add(grid);

        CreateKliker(
            name,
            activation,
            action,
            holdDuration,
            delay,
            maxDelay,
            burstCount,
            holdMode,
            toggleMode,
            burstMode,
            shouldSpam
        );
    }
    private void DeleteProfile(object sender, RoutedEventArgs e)
    {
        DeleteProfilePressed = true;
        _ = ButtonAnimator();
        var name = ProfileNameTextBox2.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        var btn = ProfilesPanel.Children
                               .OfType<Button>()
                               .FirstOrDefault(b =>
                                   string.Equals((string)b.Content, name, StringComparison.OrdinalIgnoreCase));
        if (btn == null)
            return;

        DeleteKliker(name);
        ProfilesPanel.Children.Remove(btn);

        ProfileNameTextBox2.Text = "";
        UpdateProfiles();
    }
    private void ClearAllProfiles(object sender, RoutedEventArgs e)
    {
        ClearAll();
        ClearAllPressed = true;
        _ = ButtonAnimator();
        ProfilesPanel.Children.Clear();

        _currentlyEditing = string.Empty;
        EditingLabel.Content = string.Empty;
        ProfileNameTextBox2.Text = "";
        Bind.Text = "";
        BindClick.Text = "";
        NHoldDurTextBox.Text = "";
        NdelayTextBox.Text = "";
        NMaxDelayTextBox.Text = "";
        NBurstCountTextBox.Text = "";
        HTC.IsChecked = false;
        TTC.IsChecked = false;
        PTB.IsChecked = false;
        UpdateProfiles();
    }
    private void ProfileButton(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Content is not string profileName) return;
        var clicker = GetKliker(profileName);
        if (clicker == null) return;

        _currentlyEditing = profileName;
        EditingLabel.Content = $"Editing {profileName}";
        NProfileNameTextBox.Text = clicker.ClickerName;
        var act = clicker.ActivationBind;
        Bind.Text = act.IsKey ? act.Key.ToString() : act.Mouse.ToString();
        Temp_ActivationBind = act;
        var act2 = clicker.ActionBind;
        BindClick.Text = act2.IsKey ? act2.Key.ToString() : act2.Mouse.ToString();
        Temp_ActionBind = act2;
        NHoldDurTextBox.Text = clicker.HoldDuration.ToString();
        NdelayTextBox.Text = clicker.Delay.ToString();
        NMaxDelayTextBox.Text = clicker.MaxDelay.ToString();
        NBurstCountTextBox.Text = clicker.BurstCount.ToString();
        HTC.IsChecked = clicker.HoldMode;
        TTC.IsChecked = clicker.ToggleMode;
        PTB.IsChecked = clicker.BurstMode;
        DebugNameLabel.Content = $"Name: {clicker.ClickerName}";
        DebugActivationLabel.Content = clicker.ActivationBind.IsKey ? clicker.ActivationBind.Key.ToString() : clicker.ActivationBind.Mouse.ToString();
        DebugActionLabel.Content = clicker.ActionBind.IsKey ? clicker.ActionBind.Key.ToString() : clicker.ActionBind.Mouse.ToString();
        DebugHoldDurLabel.Content = $"HoldDuration: {clicker.HoldDuration.ToString()}";
        DebugDelayLabel.Content = $"Delay: {clicker.Delay.ToString()}";
        DebugMaxDelayLabel.Content = $"MaxDelay: {clicker.MaxDelay.ToString()}";
        DebugBurstCountLabel.Content = $"BurstCount: {clicker.BurstCount.ToString()}";
        DebugHoldModeLabel.Content = $"HoldMode: {clicker.HoldMode.ToString()}";
        DebugToggleModeLabel.Content = $"ToggleMode: {clicker.ToggleMode.ToString()}";
        DebugBurstModeLabel.Content = $"BurstMode: {clicker.BurstMode.ToString()}";
        DebugThreadLabel.Content = $"Profile is on CPU Thread #{clicker.ThreadId.ToString()}";
        _originalSnapshot = new KlikerSnapshot(clicker.ClickerName, clicker.ActivationBind, clicker.ActionBind, clicker.HoldDuration, clicker.Delay, clicker.MaxDelay, clicker.BurstCount, clicker.HoldMode, clicker.ToggleMode, clicker.BurstMode);

        ToProfilePanel(sender, e);
    }
    internal bool HasUnsavedChanges()
    {
        if (_originalSnapshot == null)
            return false;
        if (_originalSnapshot.Name != NProfileNameTextBox.Text.Trim())
            return true;
        if (!ClickBindEquals(_originalSnapshot.Activation, Temp_ActivationBind))
            return true;
        if (!ClickBindEquals(_originalSnapshot.Action, Temp_ActionBind))
            return true;
        if (_originalSnapshot.HoldDuration != ushort.Parse(NHoldDurTextBox.Text))
            return true;
        if (_originalSnapshot.Delay != ushort.Parse(NdelayTextBox.Text))
            return true;
        if (_originalSnapshot.MaxDelay != ushort.Parse(NMaxDelayTextBox.Text))
            return true;
        if (_originalSnapshot.BurstCount != ushort.Parse(NBurstCountTextBox.Text))
            return true;
        if (_originalSnapshot.HoldMode != (HTC.IsChecked == true))
            return true;
        if (_originalSnapshot.ToggleMode != (TTC.IsChecked == true))
            return true;
        if (_originalSnapshot.BurstMode != (PTB.IsChecked == true))
            return true;

        return false;
    }
    internal async Task ButtonAnimator()
    {
        if (SavePressed) 
        { 
            await AnimateBorder(SaveSettingsBorder);
            SavePressed = false;
        }
        else if (LoadPressed) 
        { 
            await AnimateBorder(LoadSettingsBorder);
            LoadPressed = false;
        }
        else if (ResetPressed) 
        { 
            await AnimateBorder(ResetSettingsBorder);
            ResetPressed = false;
        }
        else if (InstallDriverPressed) 
        { 
            await AnimateBorder(InstallDriverBorder); 
            InstallDriverPressed = false;
        }
        else if (UninstallDriverPressed) 
        { 
            await AnimateBorder(UninstallDriverBorder);
            UninstallDriverPressed = false;
        }
        else if (DiscardChangesPressed) 
        { 
            await AnimateBorder(DiscardUnsavedChangesBorder);
            DiscardChangesPressed = false;
        }
        else if (SaveChangesPressed) 
        { 
            await AnimateBorder(SaveChangesBorder);
            SaveChangesPressed = false;
        }
        else if (SaveUnsavedChangesPressed) 
        { 
            await AnimateBorder(SaveUnsavedChangesBorder);
            SaveUnsavedChangesPressed = false;
        }
        else if (ClearAllPressed) 
        { 
            await AnimateBorder(ClearAllBorder);
            ClearAllPressed = false;
        }
        else if (AddProfilePressed) 
        { 
            await AnimateBorder(CreateProfileBorder);
            AddProfilePressed = false;
        }
        else if (DeleteProfilePressed) 
        { 
            await AnimateBorder(DeleteProfileBorder);
            DeleteProfilePressed = false;
        }
        else if (CoinFlipPressed) 
        { 
            await AnimateBorder(CoinFlipBorder);
            CoinFlipPressed = false;
        }
        else if (GithubPressed) 
        { 
            await AnimateBorder(GoToRepoBorder);
            GithubPressed = false;
        }
        else if (GoogleDocPressed) 
        { 
            await AnimateBorder(GoToGoogleDocBorder);
            GoogleDocPressed = false;
        }
    }
    internal async Task AnimateBorder(Border border)
    {
        if (_isAnimating) return;
        _isAnimating = true;
        try
        {
            if (border == CoinFlipBorder)
            {
                border.BorderThickness = new Thickness(2, 2, 4, 4);
                await Task.Delay(500);
                border.BorderThickness = new Thickness(1, 1, 6, 6);
            }
            else if (border == ClearAllBorder || border == CreateProfileBorder || border == DeleteProfileBorder)
            {
                border.BorderThickness = new Thickness(2, 2, 3, 3);
                await Task.Delay(200);
                border.BorderThickness = new Thickness(1, 1, 5, 5);
            }
            else
            {
                border.BorderThickness = new Thickness(2, 2, 4, 4);
                await Task.Delay(200);
                border.BorderThickness = new Thickness(1, 1, 8, 8);
            }
        }
        finally
        {
            _isAnimating = false;
        }
    }
    private void LoadSettingsbtn(object sender, RoutedEventArgs e)
    {
        LoadPressed = true;
        _ = ButtonAnimator();
        SendLogMessage("Sent load state request...");
        LoadSettings();
        UpdateProfiles();
    }
    private void SaveSettingsbtn(object sender, RoutedEventArgs e)
    {
        SavePressed = true;
        _ = ButtonAnimator();
        SendLogMessage("Sent save state request..");
        SaveSettings();
        UpdateProfiles();
    }
    private async void CoinFlip(object sender, RoutedEventArgs e)
    {
        CoinFlipPressed = true;
        _ = ButtonAnimator();
        if (!CoinReady) return;
        CoinLabel.Content = "...";
        CoinReady = false;
        await Task.Delay(500);
        var result = RandomGenerator.Next(2) == 0 ? "Heads" : "Tails";
        CoinLabel.Content = result;
        CoinReady = true;
    }
    private void ToggleLowLevelInput(object sender, RoutedEventArgs e)
    {
        if (!InputInterceptor.CheckDriverInstalled())
        {
            errborder.Background = Brushes.Red;
            errlabel.Content = "Error, check log";
            SendLogMessage("you must install the InputInterceptor driver before you can enable this option.");
            return;
        }
        LowLevelInput = !LowLevelInput;
        LowLevelInputCheckBorder.BorderThickness = LowLevelInput ? new Thickness(2, 2, 4, 4) : new Thickness(1, 1, 8, 8);
        SendLogMessage($"Low-Level Input is now {(LowLevelInput ? "enabled" : "disabled")}");

        UpdateProfiles();
    }
    private void ToggleUpdateChecking(object sender, RoutedEventArgs e)
    {
        AllowUpdates = !AllowUpdates;
        AllowUpdateCheckingCheckBorder.BorderThickness = AllowUpdates ? new Thickness(2, 2, 4, 4) : new Thickness(1, 1, 8, 8);
        SendLogMessage($"Oki i {(AllowUpdates ? "will" : "wont")} check for updates");
    }
    private void ToggleCustomTitles(object sender, RoutedEventArgs e)
    {
        UseCustomTitles = !UseCustomTitles;
        UseCustomTitlesCheckBorder.BorderThickness = UseCustomTitles ? new Thickness(2, 2, 4, 4) : new Thickness(1, 1, 8, 8);
        SendLogMessage($"Okaaaa i {(UseCustomTitles ? "will" : "wont")} check for titles in the google doc");
    }
    private async void InstallDriver(object sender, RoutedEventArgs e)
    {
        InstallDriverPressed = true;
        _ = ButtonAnimator();
        if (InputInterceptor.CheckDriverInstalled())
        {
            errborder.Background = Brushes.Red;
            errlabel.Content = "Error, check log";
            SendLogMessage("Driver is already installed!!1");
            await Task.Delay(2500);
            errborder.Background = Brushes.White;
            errlabel.Content = "...";
        }
        else if (!InputInterceptor.CheckAdministratorRights())
        {
            errborder.Background = Brushes.Red;
            errlabel.Content = "Error, check log";
            SendLogMessage("To install the Driver you need to run the app in administrator :c");
            SendLogMessage("i know scary, but is optional!!");
            await Task.Delay(2500);
            errborder.Background = Brushes.White;
            errlabel.Content = "...";
        }
        else if (InputInterceptor.CheckAdministratorRights() && !InputInterceptor.CheckDriverInstalled())
        {
            InterceptorInputSender.InstallDriver();
            errborder.Background = Brushes.Green;
            errlabel.Content = "Check Log";
            SendLogMessage("The driver was installed, to finish the installation you need to restart your computer.");
        }
    }
    private async void UninstallDriver(object sender, RoutedEventArgs e)
    {
        UninstallDriverPressed = true;
        _ = ButtonAnimator();
        if (!InputInterceptor.CheckAdministratorRights())
        {
            errborder.Background = Brushes.Red;
            errlabel.Content = "Error, check log";
            SendLogMessage("To Uninstall the Driver you need to run the app in administrator :c");
            SendLogMessage("i know scary, but is optional!!");
            await Task.Delay(2500);
            errborder.Background = Brushes.White;
            errlabel.Content = "...";
        }
        if (!InputInterceptor.CheckDriverInstalled())
        {
            errborder.Background = Brushes.Red;
            errlabel.Content = "Error, check log";
            SendLogMessage("driver no installed already :3");
            await Task.Delay(2500);
            errborder.Background = Brushes.White;
            errlabel.Content = "...";
        }
        if (InputInterceptor.CheckAdministratorRights() && InputInterceptor.CheckDriverInstalled())
        {
            InterceptorInputSender.UninstallDriver();
            errborder.Background = Brushes.Green;
            errlabel.Content = "Check Log";
            SendLogMessage("The driver was uninstalled, to finish the uninstallation you need to restart your computer.");
        }
    }
    private void UnsavedSave(object sender, RoutedEventArgs e)
    {
        SaveUnsavedChangesPressed = true;
        _ = ButtonAnimator();
        SaveProfileChanges(sender, e);
        UnsavedChangesBorder.Visibility = Visibility.Collapsed;
        ToKlikerPanelY(sender, e);
    }
    private void UnsavedDiscard(object sender, RoutedEventArgs e)
    {
        DiscardChangesPressed = true;
        _ = ButtonAnimator();
        if (_originalSnapshot == null)
            return;

        Temp_ActivationBind = _originalSnapshot.Activation;
        Temp_ActionBind = _originalSnapshot.Action;
        _currentlyEditing = _originalSnapshot.Name;
        NProfileNameTextBox.Text = _originalSnapshot.Name;
        Bind.Text = _originalSnapshot.Activation.IsKey ? _originalSnapshot.Activation.Key.ToString() : _originalSnapshot.Activation.Mouse.ToString();
        BindClick.Text = _originalSnapshot.Action.IsKey ? _originalSnapshot.Action.Key.ToString() : _originalSnapshot.Action.Mouse.ToString();
        NHoldDurTextBox.Text = _originalSnapshot.HoldDuration.ToString();
        NdelayTextBox.Text = _originalSnapshot.Delay.ToString();
        NMaxDelayTextBox.Text = _originalSnapshot.MaxDelay.ToString();
        NBurstCountTextBox.Text = _originalSnapshot.BurstCount.ToString();
        HTC.IsChecked = _originalSnapshot.HoldMode;
        TTC.IsChecked = _originalSnapshot.ToggleMode;
        PTB.IsChecked = _originalSnapshot.BurstMode;
        SendLogMessage($"Discarded unsaved changes for {_originalSnapshot.Name}");
        UnsavedChangesBorder.Visibility = Visibility.Collapsed;
        ToKlikerPanelY(sender, e);
    }

    private void GoToGoogleDoc(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://docs.google.com/document/d/1wtMBiTUCG-tkKZLOrKfAzlp2NkPbEyrz6Jtmb8w-Hhg",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            SendLogMessage($"Failed to open google doc: {ex.Message}");
        }
    }

    private void GoToRepo(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Iris-Belle/Iris-Auto-Cliker",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            SendLogMessage($"Failed to open github repo: {ex.Message}");
        }
    }

    private async void CheckDriver(object sender, RoutedEventArgs e)
    {
        if (InputInterceptor.CheckDriverInstalled())
        {
            errborder.Background = Brushes.Green;
            errlabel.Content = "Installed";
            await Task.Delay(2500);
            errborder.Background = Brushes.White;
            errlabel.Content = "...";
        }
        else
        {
            errborder.Background = Brushes.Red;
            errlabel.Content = "Not Installed";
            await Task.Delay(2500);
            errborder.Background = Brushes.White;
            errlabel.Content = "...";
        }
    }
}
