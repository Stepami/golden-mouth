using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public enum LiveEventType
{
    Views,
    React,
    Comment,
    Typing,
    Result,
}

