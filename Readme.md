# Puck

Puck is a Discord bot that lists, pings for, and delists LFG posts.

The purpose of the bot is to be able to ping a large group of people for an
event, and then automatically archive the thread (also removing the ping),
so people who missed the event don't see outdated pings.

Puck works out-of-the-box, but is also completely configurable! Almost all
behavior is customizable. **Puck will not ping any roles out-of-the-box,**
until you confirm them / set them up through `/config mention`.

`/help` will display a complete guide to using the bot; `/config help` will
display a guide to setting up the bot on your server.

*You can also find this bot at:*
- **[Top.gg][2]**
- **[Discord Bot Labs][3]**
- **[Discord Bots][4]**
- **[Discord Bot List][5]**



## Setup

To add the bot to your server, use **[this link][1]**.

The required permissions are:
- View Channels - *to check channel settings*
- Send Messages - *to post additional information*
- Create Public Threads - *to create new LFG posts*
- Send Messages in Threads - *to post updates to LFG posts*
- Manage Threads - *to archive expired LFG posts*
- Embed Links - *to properly format LFG posts*
- Read Message History - *to recover from interrupted connections*
- Mention Everyone - *to ping roles in LFG posts*
- Use External Emojis - *to use custom icons*

Once you add the bot to your server, you can use the `/config help` command
to view a setup guide. You can use `/config view` to check the current
settings at any time.

**You can restrict which channels Puck can post in** by updating your server
settings. Go to: "Server settings" -> "Integrations" -> "Puck", and restrict
where the command `/lfg` can be run.

- `/config default-group-type` will set the group type to be used, when not
  otherwise specified (either explicitly or by mentioning a role). This
  **can** be set to a group type otherwise disabled through
  `/config group-types`.
- `/config default-duration` will set the time before archiving an LFG post,
  when not specified. Each LFG post can still be individually extended after
  posting.
- `/config group-types` allows you to select which group types can be chosen
  from. (See also: `/config default-group-type`.)
- `/config mention list` will display the roles that Puck is allowed to ping.
  - `/config mention set` will allow Puck to ping a role, and set the default
    group type to use for that role.
  - `/config mention remove` will stop Puck from being able to ping a role.



## Usage

Using the `/lfg` command will create an LFG post in a new thread, in the
channel where the command was used. Once a post has been created, the original
poster can configure it, and others can sign up for the group.

### Setting up the group

Press the role buttons to pre-fill spots in your group. If the maximum number
of that role is already in your group, the pre-filled spots will cycle back
to 0 and start again. Press the cancel button to clear **all** pre-filled
spots.

As the original poster of the group, you can add time to the listing (the
time before the post is automatically delisted), or delist the group early
(e.g. the group is full or no longer looking for members).

### Signing up for a group

To sign up for a role, simply press the corresponding role button. If you've
already signed up for a different role, your role will be switched to the
new one. Press the cancel button to open your spot back up.



## Hosting

The actual bot supports an arbitrary number of servers from a single instance.
The following instructions are for hosting *your own instance of the bot*,
which shouldn't usually be necessary. The following instructions assume some
knowledge of how to host a bot.

To host your own instance of the bot, you will need to create a new bot from
Discord's developer portal, and add *that* bot to your server. You will also
need to add the bot token to a blank `token.txt`, and place it inside
`Puck/config/`. Also create the file `token_debug.txt` in the same folder,
and add either the same token, or a token for a different bot application
(that you created by repeating the above steps).

The bot will automatically connect to Discord using the debug token, if run
in debug mode. This allows for testing changes without needing to take the
main bot down.

**Do NOT share these tokens,** e.g. by uploading them to GitHub. Both token
files are ignored in git (see the `.gitignore` file).

After building the project, you will also need to create the databases for
the bot. Use the Entity Framework package manager tools (either in Visual
Studio, or the CLI tools) to update the databases. Once they're created,
place them inside `Puck/data/`.

Copy the entire `/config/` and `/data/` folders to the build output directory,
and you should be able to run the bot.



[1]: https://discord.com/api/oauth2/authorize?client_id=703068724818608138&permissions=326417992704&scope=applications.commands%20bot
[2]: https://top.gg/bot/703068724818608138
[3]: https://bots.discordlabs.org/bot/703068724818608138
[4]: https://discord.bots.gg/bots/703068724818608138
[5]: https://discordbotlist.com/bots/puck
