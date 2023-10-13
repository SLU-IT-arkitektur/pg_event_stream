## eventspurger

A simple c# application that deletes events from our events table that are older than a configurable time(number * unit).
it takes two arguments: **unit** (*days or hours or minutes*) and a **number** (int).

If you run it locally this can look like this:
```
dotnet run -u minutes -n 1
```
The idea is for this eventspurger to run in a container in a kubernetes CronJob, both locally with Skaffold and in our test and production clusters.

How often it runs can be configured in the CronJob yaml file.
The unit (-u) and number (-n) arguments can be configured in a ConfigMap yaml file.

Make sure the eventspurger and your postgres database are using the same timezone.