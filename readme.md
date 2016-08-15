Team Coding
--

**About:** The Team Coding Visual Studio extension helps discover team members who are working in the same area of a solution as you. You can set your own username and user image url via settings (Tools->Options->Team Coding), or let it try and figure them out automatically (see below). Once working it will display user icons at the top of your tabs when other users have the same document open, with a white border when the tab is selected and a grey border when it's edited.

**Settings:** In the Team Coding settings page are configuration options for how to share open document information with others as well as options for customising how you appear to others. Multiple share methods can be used at once, you can also add a teamcoding.json file anywhere within the solution folder (typically from the repo.) for it to override user settings. If this file is in place then for that solution settings don't need to be set in each developer's IDE.

**Roadmap:** Still very much alpha, please report bugs as [GitHub issues](https://github.com/georgeduckett/TeamCoding/issues). Currently sharing options are a shared folder, and Redis. Will initially support Git repos for determining what to share. In the pipeline is support of other source control systems, as well as a shared-coding experience (multiple users editing the same document). I'm also considering a seperate application or window to get an overview of all user's activity without having to have the same tabs open.

**User Identity:** It tries to get your email address as a user identity from various sources (saved Windows Credentials for GitHub, your logged in identity from visual studio, your machine name). This will be made public to your team. It uses Gravatar to get a user image from the email address.

**Links:** [GitHub Repo](https://github.com/georgeduckett/TeamCoding/)