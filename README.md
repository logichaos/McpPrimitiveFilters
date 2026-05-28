# Authenticated http MCP server with tool selection

## credits

- [erwinkramer](https://github.com/erwinkramer) is the author of the [bank-api](https://github.com/erwinkramer/bank-api) project. If you don't know the repo, have a look at it. It's a great place to learn many things about writing APIs in AspNetCore.
  - A while back I was in need of refreshing my knowledge about modern .net web APIs and landed on that repo. It has been a huge influence on how I write web apps and a strong guidelie on how to structure my codde.
  - When I contacted erwinkramer about wanting to publish my MCP project that was influenced by what I learned from his repo, he not only told me that is was ok, he even took time to review my repo and gave me a lot of pointers and feedback. He made me realize a lot of things that I hadn't thought of and lead to the decision to rewrite the whole thing from scratch.
- [Mario Zechner](https://mariozechner.at) is the author of the [PI coding agent](https://pi.dev). I love PI because, to me, it emphasizes the importance of keeping the context concise.
  - In some of his blogs, Mario delves into MCP servers and how they clutter your context with information about all the tools that you don't need. (please check out his blog for more info, it was a really good read for me and I think it could be for many other people)
  - What he said resonated with my strong conviction, that controlling the context is something that many people should understand, in order to get better results from AI models: too much might lead to hallucinations, too little might give wrong results.
  - That is why I thought of the idea of having an MCP server, where you could just choose which tools are exposed, to keep your context concise.

## tl;dr

- This repo contains my implementation of an http MCP server, built on top of the [modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk), where the exposed tools can be controlled. So you can expose only selected tools from the server, depending on your need or authorization from an enterprise OAuth server.

## What is inside

- (WIP) A copy of the TestOAuthServer from the csharp-sdk tests, with modifications to show how to control what tools are accessible using scope claims returned by the server.
- An Http MCP server which can inject different strategies to filter the exposed tools
- (WIP) An example strategy which uses appsettings to control which tools are exposed by the server. (ex. giving someone who runs it locally a direct way to control which tools to show)
- (WIP) An example strategy using JWT bearer scope claims to filter the tools using the supplied scope claims by the OAuth server. (ex. giving someone in an enterprise setting a way of controlling which tools someone can access)

## Disclaimer

- This is a repo for me to learn more about the sdk, as well as the capabilities of MCP, I try hard to make it "production ready", so if you think something is missing, don't hesitate to create an issue.
- I can't promise to have time for all feedback, but, as long as I do, I will ;-)
- I used AI assisted code generation for the project. The way I use it is more of a "learner". If I get stuck on something, I brainstorm with the agent and I have a solution proposed/implemented, then I go through all generated code and try to understand how it works/propose changes, according to my engineering skills. Once I have a good grasp of what needs to be done, I scratch everything and start anew, with the learnings I have made from the previous run, along with unit and integration tests.
- I have created this code from scratch many times, and only when I reach a point that I am satisfied with, will I have a snapshot upon which I will do the whole rinse-and-repeat cycle all over again.
- This is my first public repo with code that I created. It's scary, but I would be very happy if anyone can use what I learned here to make their lives easier.
- Along with learning about MCP, I have decided to try and use [JJ vcs](https://www.jj-vcs.dev/latest/) for version control.
  - If you don't know anything about JJ, I encourage you to have alook, as it has proven to be really powerful and fun to use. This coming from someone who has been a hardcore git fan, using the awesome [LazyGit client](https://github.com/jesseduffield/lazygit). LazyGit is, for me, the best git client I have every seen. It has upped my git game in so many ways and I'm forever grateful for Jesse for this amazing tool.

## About the License

- Since it's my first time making a public repo, I am unsure what license to use. I wanted something that is open for people to use, but was thinking that it might be nice to have changes flow back into the repo, in order to help everyone.
- After a bit of research, it was suggested to me, that a MPL-2.0 would do exactly that, so I went with it.
- If you have any input on this, I would be very happy to hear it. I'm a total newbie here.
