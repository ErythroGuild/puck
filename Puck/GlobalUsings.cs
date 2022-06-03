// System.
global using System;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.Diagnostics;
global using System.IO;
global using System.Threading.Tasks;

// D#+.
global using DSharpPlus;
global using DSharpPlus.Entities;

// Microsoft.
global using Microsoft.Extensions.Logging;

// Serilog.
global using Serilog;

// Project namespaces.
global using Puck.Database;
global using Puck.Utils;

// Static usings.
global using static Puck.Utils.Util;

// Aliases.
global using Command = DSharpPlus.Entities.DiscordApplicationCommand;
global using CommandOption = DSharpPlus.Entities.DiscordApplicationCommandOption;
global using CommandChoice = DSharpPlus.Entities.DiscordApplicationCommandOptionChoice;
global using InteractionArg = DSharpPlus.Entities.DiscordInteractionDataOption;
