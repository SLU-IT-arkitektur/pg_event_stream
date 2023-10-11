## eventspurger

A simple c# application that deletes events from our events table.
it takes two arguments: **units** (*days or hours or minutes*) and a **number** (int).

If you run it locally this can look like this:
```
dotnet run -u minutes -n 1
```
The idea is for this eventspurger to run in a container in a kubernetes CronJob, both locally with Skaffold and in our test and production clusters.

How often it runs can be configured in the CronJob yaml file.
How many events it deletes can be configured in the ConfigMap yaml file.