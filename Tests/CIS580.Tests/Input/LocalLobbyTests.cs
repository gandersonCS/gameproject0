using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using CIS580.Input;

namespace CIS580.Tests.Input;

public class LocalLobbyTests
{
    private readonly FakeInput _fake = new();
    private readonly InputSource _left = new(InputDevice.KeyboardMouse, StandardSchemes.KeyboardLeft());
    private readonly InputSource _right = new(InputDevice.KeyboardMouse, StandardSchemes.KeyboardRight());
    private readonly InputSource _pad = new(InputDevice.ForGamePad(1), StandardSchemes.GamePad());
    private readonly LocalLobby _lobby;
    private readonly List<(LobbySeat Seat, LobbyChange Change)> _changes = new();

    public LocalLobbyTests()
    {
        _lobby = new LocalLobby(new[] { _left, _right, _pad });
        _lobby.Changed += (seat, change) => _changes.Add((seat, change));
    }

    /// <summary>
    /// Presses keys for one frame (then releases them), updating the lobby each frame
    /// </summary>
    private bool Press(params Keys[] keys)
    {
        _fake.Keys(keys);
        bool changed = _lobby.Update(_fake.State);
        _fake.Keys();
        _lobby.Update(_fake.State);
        return changed;
    }

    [Fact]
    public void Confirm_JoinsWithThatSource()
    {
        Assert.True(Press(Keys.Enter));     // Right keyboard confirm

        var seat = Assert.Single(_lobby.Seats);
        Assert.Same(_right, seat.Input);
        Assert.False(seat.IsReady);
        Assert.Equal((seat, LobbyChange.Joined), Assert.Single(_changes));
    }

    [Fact]
    public void TwoPlayers_CanShareTheKeyboard()
    {
        Press(Keys.Space);
        Press(Keys.Enter);

        Assert.Equal(new[] { _left, _right }, new[] { _lobby.Seats[0].Input, _lobby.Seats[1].Input });
    }

    [Fact]
    public void AGamePad_CanJoin()
    {
        _fake.Pads(FakeInput.Pad(), FakeInput.Pad(buttons: Buttons.A));
        _lobby.Update(_fake.State);

        Assert.Same(_pad, Assert.Single(_lobby.Seats).Input);
    }

    [Fact]
    public void ConfirmAgain_MarksReady_AndCancelUnreadies_ThenLeaves()
    {
        Press(Keys.Space);
        var seat = _lobby.Seats[0];

        Press(Keys.Space);
        Assert.True(seat.IsReady);

        Press(Keys.Q);
        Assert.False(seat.IsReady);
        Assert.Single(_lobby.Seats);

        Press(Keys.Q);
        Assert.Empty(_lobby.Seats);
        Assert.Equal(new[] { LobbyChange.Joined, LobbyChange.Ready, LobbyChange.NotReady, LobbyChange.Left },
            _changes.ConvertAll(c => c.Change));
    }

    [Fact]
    public void JoiningPress_DoesNotAlsoMarkReady()
    {
        Press(Keys.Enter);
        Assert.False(_lobby.Seats[0].IsReady);
    }

    [Fact]
    public void CanStart_NeedsEnoughPlayers_AllReady()
    {
        _lobby.MinPlayers = 2;
        var a = _lobby.Join(_left);
        _lobby.SetReady(a, true);
        Assert.False(_lobby.CanStart);      // Not enough players

        var b = _lobby.Join(_right);
        Assert.False(_lobby.CanStart);      // Not everyone is ready

        _lobby.SetReady(b, true);
        Assert.True(_lobby.CanStart);
    }

    [Fact]
    public void AFullLobby_IgnoresMoreJoins()
    {
        var lobby = new LocalLobby(new[] { _left, _right }, maxPlayers: 1);
        lobby.Join(_left);

        _fake.Keys(Keys.Enter);
        Assert.False(lobby.Update(_fake.State));
        Assert.Single(lobby.Seats);
        Assert.Throws<InvalidOperationException>(() => lobby.Join(_right));
    }

    [Fact]
    public void Available_ExcludesJoinedSources()
    {
        _lobby.Join(_pad);
        Assert.Equal(new[] { _left, _right }, _lobby.Available);
    }

    [Fact]
    public void CancelFromSomeoneWhoHasntJoined_ChangesNothing()
    {
        // So the state showing the lobby can treat it as "go back"
        Assert.False(Press(Keys.Q));
    }

    [Fact]
    public void DescribeBindings_ListsEachInputOnce()
    {
        var player = new InputSource("Player",
            new InputSource.Assignment(InputDevice.KeyboardMouse, StandardSchemes.Keyboard()),
            new InputSource.Assignment(InputDevice.ForGamePad(0), StandardSchemes.GamePad()));

        Assert.Equal("Esc / Start", player.DescribeBindings(StandardControls.Pause));
    }
}
