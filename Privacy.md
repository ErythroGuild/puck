# Privacy Policy

This is an explanation of how Puck collects and stores user data. We
try to store as little data as possible, and all data that *is* stored
is directly used to provide the expected functionality. As a result,
**none of the stored data is personally identifiable,** and you can
always remove what little data is stored.

All source code is open-source and available on GitHub:
https://github.com/ErythroGuild/puck

## What data is collected?

Puck collects and stores 3 main types of data. These are all critical
to the advertised functionality, and are used as expected.

1. Any data needed to reconstruct an individual listing is stored for
   as long as the listing is active. This allows Puck to be tolerant
   of disconnects, and continue functioning normally in spite of any
   service interruptions. It also allows Puck to be restarted when
   necessary (e.g. for upgrades, maintenance, or troubleshooting).
   This includes:
   - the guild, thread, and message ID of a listing,
   - any data on the listing itself, and
   - the user IDs of the members signed up for the listing.
2. Any settings that any individual Discord server has used to
   customize Puck for their own server. This includes:
   - the guild ID and name,
   - the settings themselves (e.g. default values), and
   - any role IDs used in these settings.
3. Listings are logged, but anonymized and only associated with guild
   IDs. The **only** data logged is the timestamp of the listing
   creation. This is **not** personal or user data, and attempting to
   reconstruct or use the data in a malicious way would require
   joining the server in question. This data is used to monitor and
   troubleshoot Puck, and for scheduling maintenance and upgrades.
   - User IDs (or any personal data) is **not** logged.
   - Guild IDs and listing creation timestamps *may* be logged.

## How long is it stored?

Ephemeral data (data used to reconstruct listings) is only stored for
as long as the associated listing is active. This is typically only a
few minutes.

Guild-associated data (configuration data and timestamp statistics)
are only stored for as long as Puck is in the associated Discord
server. All data is removed when Puck is removed from the Discord
server.

## How is it protected?

All of the data is hosted on a secure Linode VPS, and may be backed
up. All relevant accounts are protected by recommended and standard
security measures.

## What if something happens?

No data is stored which is personally identifiable. Nonetheless, data
security is treated seriously, and any breaches will be disclosed on
the Discord support server, as well as having fixes addressed in the
next earliest release notes on GitHub.

## How can I remove my data?

No data is stored which is personally identifiable.
- Any ephemeral data (data used to reconstruct listings) can be
  removed by delisting the associated listing.
- Any guild-related data can be removed by removing Puck from the
  associated discord servers.

## What if I would like more information?

There are multiple ways to contact us.
- GitHub issues: https://github.com/ErythroGuild/puck/issues
- GitHub discussions: https://github.com/ErythroGuild/puck/discussions
- You can also message Ernest on Discord.

[1]: https://github.com/ErythroGuild/puck/blob/master/Privacy.md
[2]: https://github.com/orgs/ErythroGuild/people
[3]: https://github.com/ErythroGuild/puck
[4]: https://github.com/ErythroGuild/puck/blob/master/License.txt
