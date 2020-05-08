# Puck

Puck is a Discord bot that lists, pings, and delists LFG notifications.

The purpose of the bot is to be able to ping a large group of people for an
event, and then automatically delete the ping (after a timer) so people who
missed the event don't see outdated pings.

There are some convenience features as well (e.g. DM notifications).

You can also find this bot at:
- **[Discord Bot List][2]**
- **[Discord Bots][3]**
- **[Bots on Discord][4]**

## Setup

To add the bot to your server, use **[this link][1]**. The required permissions are:
- Read Messages - *to read commands*
- Send Messages - *to post groups*
- Manage Messages - *to use reactions as buttons*
- Mention Roles - *to ping roles in groups*
- Add Reactions - *to setup reaction buttons*
- Use External Emojis - *to use custom reactions*

Once you add the bot to a server, it will DM you setup instructions. You can
configure the bot either by replying to the bot, or by messaging in any channel
the bot has read permissions for. There are only two options:
1. `@Puck -config channel {channel-name}`
2. `@Puck -config mention {role-name}`

You can view the current settings with:
- `@Puck -config view`

**The bot will NOT respond to any messages** until a default channel is set up.
However, the second command is optional. If a default mention isn't set, then
it'll be set as "none" by default. These can also be changed later by issuing
the same commands.

Be careful not to actually mention the channel name or role (e.g. `#lfg` or `@keys`).

**Example:**

`@Puck -config channel pve-lfg`

This sets the bot to respond in the channel `#pve-lfg`, and by default the bot
will *not* mention any roles when creating groups (if one isn't specified).

If you are the owner of multiple servers that contain this bot, and the config
command was issued through a DM, you will need to specify which server you are
configuring the bot for. E.g.:

`@Puck -config <Erythro> channel lfg`

## Usage

The command structure of the bot is simple.

`@Puck [-{type}] [!{mention}] {group-title}`

The `-{type}` and `!{mention}` are optional, but if they are both present,
must be in the above order.

The following is a list of recognized group types:
- Dungeon: `-dungeon`, `-mythics`, `-m0`, `-key`, `-m+`, ...
- Raid: `-raid`
- Warfront: `-warfront`, `-wf`
- Arenas: `-arenas`, `-2v2`, `-2s`, ...
- RBG: `-rbg`, `-ratedbg`, `-ratedbattleground`
- Battleground: `-battleground`, `-bg`, `-brawl`
- Vision: `-vision`, `-hv`
- Scenario: `-scenario`
- Island: `-island`
- Other: `-other`, `-misc`

Mentions can be any role in the server and should be typed without an `@`.
E.g. `!everyone`, not `!@everyone`. If no mention is specified, the default for
the server is used, if one has been specified. `!none` can be used to suppress
pings if a server default has been configured.

You can mute/unmute bulletin notifications from the bot with the following pair
of commands:
- `@Puck -mute`
- `@Puck -unmute`

Once a group has been listed, you can use the reaction buttons to set up the
group (depending on the type of group). Pressing the 🔄 reaction will keep the
group listed for an additional 5 minutes, and pressing the ✅ reaction will
immediately delist the group.

You can also edit/delete your original post to update the corresponding
bulletin, *as long as* that bulletin has not been delisted yet.

## Hosting

The actual bot supports an arbitrary number of servers from a single instance.

If you would like to run your own version of the bot, you will need to create a
new bot from Discord's developer portal,  and add it to your server using your
own client ID. (This bot's ID is `703068724818608138`, for reference.) In
addition to running the code, you will need to add a `token.txt` file in the
executable directory, containing a single line with your bot's token.

**Do NOT share this token,** especially by uploading it to GitHub. The
`token.txt` file is globally ignored through the `.gitignore` file by default.

## Project Structure

The code for this project consists of a number of different separated
classes. All components are well-documented with comments in their
respective `.cs` files, and the functioning of each component should be
self-explanatory. If anything is *not* evident, consider opening a
GitHub issue to resolve it.

- **`Program` defines the main program.** This deals with all of the
interfacing between the Discord API, user input, and console output.
This is the entry point of the program.
- `Format` defines methods (and extension methods) that are syntactic
sugar for Discord-supported text formatting. These are all "dumb"
functions and do not attempt to perform any validation of IO.
- `Util` contains helper functions that could be useful in any program
(not just this project).
- `Logger` unifies all console output with a single class. Although it
is *not* a singleton, each program should instantiate a single one and
pass it to its children.
- `Blocklist` is a lightweight `HashSet<>` wrapper for a list of user
IDSs not to notify for listing-related events.
- `Emoji` is a wrapper around `DiscordEmoji` that allows strongly-typed
access. It attempts to use custom emojis (from the **Erythro** server),
but will fall back to default Unicode emojis.
- `Settings` defines a set of configurations for a particular server.
- `Bulletin` contains the data associated with a particular bulletin,
created from a user command.
- `BulletinData` contains the data associated with a particular bulletin,
which was inputted by a user and parsed.
- `Group` holds the number and type of members associated with any
bulletin. It also contains relevant type functionality.
- `MentionRole` is a wrapper around `DiscordRole` to also include "none",
`@here`, and `@everyone` (since Discord does not consider those actual
`DiscordRole`s to mention).

[1]: https://discordapp.com/oauth2/authorize?client_id=703068724818608138&scope=bot&permissions=404544
[2]: https://top.gg/bot/703068724818608138
[3]: https://discord.bots.gg/bots/703068724818608138
[4]: https://bots.ondiscord.xyz/bots/703068724818608138
