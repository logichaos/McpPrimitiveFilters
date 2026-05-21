# AuthenticatedHttpMcpServer

## tl;dr

An Http MCP server with authentication and the ability to choose which tools are listed.

## Disclaimer

- A HUGE thank to  [erwinkramer](https://github.com/erwinkramer) for his amazing [bank-api](https://github.com/erwinkramer/bank-api). If you don't know what it is, go and check it out, it's a diamond mine of information if you're trying to learn more about modern .net web API applications. Thank you Erwin, you're the reason I am making this public!
- I have, blatantly, copied a lot of the work that Erwin has done, and dare not take any credits for the stuff that I  stole from him!
- I am writing this project to try and learn more about MCP servers in .net.
- Any and all feedback is welcome. I hope that it can be helpful to anyone out there getting started with Http MCP servers in .net
- I used AI assisted code generation for some parts because my main focus was understanding the workings of the MCP C# SDK.
- This is my first real project to make public on GitHub, and I'm scared/excited, but I'm willing to go through with it, because I want to give back! 
- I can't promise to have much time for issues, but, as long as I do have time, I'll try!
- I am learning to use jj vcs and will be using it exclusively for this project. If the commits are weird, it may be because of my lack of understanding of jj so far.
  - If you don't know what jj is, give it a try, I think it's amazing!! [Jujutsu VCS](https://www.jj-vcs.dev/latest/)

## Goal

The main goal is to learn about the C# MCP SDK and how it can be used in a real-world situation
The second goal is to make a base structure for an MCP server that can be used by others to save them some time

## What is included

### Authentication

- I copied the work of [Erwin](https://github.com/erwinkramer) in the [bank-api](https://github.com/erwinkramer/bank-api)
- I added API (Azure) authentication using the ApiKeyAuthHandler
  - this is a little change from how Erwin does it, but I was attempting to make it so that the API authentication code can be tested in an easier way (I am unaware of easy ways to unit test Hosts)
  - I didn't know about AuthenticationHandler<<T>> before I attempted this. If you have any feedback, don't hesitate to write an issue

- I added JWT authentication in a similar manner
  - Various options for the token validation can be configured from the appsettings.json file, have a look at them if you need a change
  - The Roles for the JWT authentication are hard-coded in the application at the moment. I will change them to something configurable from the appsettings.json file in the future.

- I added a health check that requires authentication

- I added rate limits to play around with i
  - right now, there is only a simple Fixed window rate limit applied
  - in the future, I will attempt to play around with some other options
    - so far, my intended use for this is an MCP server that I don't think will be/should be called so much, but adding a rate limit sounded like a sane thing to do for something that might be exposed to the internet, or in a network with malicious actors 

- I added a means of exposing specific tools to a user, using scopes. So far it only works for JWT and is still a work in progress

#### Explanation

I am a big fan of [PI](https://pi.dev) and its attempt to stay minimalistic. 
In one of his blogs, [Mario Zechner](https://mariozechner.at/), the creator of PI, [wrote about MCP vs CLI](https://mariozechner.at/posts/2025-08-15-mcp-vs-cli/). The content of that article resonated with me quite a bit because, for a wile now, I have been convinced that managing your context is a skill that you need to master, if you want your model to generate meaningful results. If you have too many MCP tools, your context is already cluttered with things that you probably don't need. I will not go into details about what Mario said in the article (please go read it yourself!), but I got to thinking: If it's possible to have an MCP tool expose only the tools that you need, it might be a step in the right direction for keeping the context clean. Also, it wouldn't hurt to be able to define who gets access to which tools. So I thought to myself: that would be awesome to achieve.

I thought I was on my way to implement the hack of the month for MCP, since I don't think that the MCP specification includes anything about this. HOWEVER, it seems that the awesome people who wrote the C# MCP SDK thought of that already!

First I tried doing it with the builder.Services.AddMcpServer().WithToolsListToolsHandler(...), but that rabbit hole was a bit too complicated for what I needed. I didn't want to create the Tool objects for each tool that I have, manually! It seems that this road is one for someone who wants to go a bit more low-level than what I am willing to go right now.
It is good to know that it's possible, tho!

After a bit of searching, I found the AspNetCoreMcpPerSessionTools class in the SDK code and that turns out to do exactly what I wanted without too much of a hassle.
I used the example and added a way to filter out the tools using the scope of the jwt bearer token.
This way, if you're creating the token for your users, you can decide what tools they can use (think of read-users, write-users, and admins) or, if you are running the tool on your own network and want to optimize the tokens for your agent, you can give it a token that has only the tools that you know you need
