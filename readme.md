# Proof-of-Concept: Using PostgreSQL as an Event Stream Solution

This is a proof-of-concept for using postgres as an event stream with one or more producers and multiple consumers using topics and the NOTIFY and LISTEN commands.

**This is a work in progress!**

## CONTEXT AND REASONING

For a few projects we see the need to use event driven architecture and event streaming. We have been looking at introducing RabbitMQ for example but we do, however, want to keep the technology stack as simple as possible (both in terms of developer skills needed and in terms of operational complexity). Postgres is our default choice when it comes to relational databases and i already in use in most teams. The idea with this POC is to evaluate if using PostgreSQL as an event stream solution is "good enough" for our needs. The purpose is to utilize technology **already established** in-house when it suffices and not, in this particular case, to try and re-implement a full-blown event streaming platform. It is, like with so many things, a matter of trade-offs when to introduce something new and when to stick to something you already have and know. This POC is an interesting experiment and we will see where it takes us this time. I for one think there is something to say for [choosing "boring" technology](https://boringtechnology.club/).


## SYSTEM IN POC

**db:** postgres database for events (aka the eventsdatabase)

**db_migrations:** applies migrations to the eventsdatabase (events table and roles 
and permissions for the producer and the consumer)

**pgadmin:** postgres admin tool (webbased)

**producer:** .net 6 console app. produces events by writing to the events table in our eventsdatabase (postgres) and emitting a NOTIFY event on the channel "events". 

**webconsumer:** .net 6 web app. Example application consuming the events. Subscribes to the channel "events" with LISTEN and fetches events of interest (based on topics) from the events table in our eventsdatabase (db). Keeps track of received events and can be stopped and started again without missing events.

**webconsumerdb:** MongoDB. the example consumers database.

**eventspurger:** purges events from the eventsdatabase (db) after a certain amount of time. configurable. ***LEFT TO IMPLEMENT***

## HOW IT WORKS

The producer writes events to the events table in the eventsdatabase (db) and emits a ``NOTIFY`` notification on the channel "events" for every event. The webconsumer subscribes to the channel "events" with ``LISTEN`` and fetches events of interest (based on topics) from the events table in the eventsdatabase (db). The webconsumer keeps track of received events and can be stopped and started again without missing events since it will fetch any missed events when it starts up. It also fetches any new events when successfully reconnecting after a connection loss. The postgres event stream can store events for a configurable amount of time, after which they are purged. ***LEFT TO IMPLEMENT***.

PostgresEventSubscriber is a generic asp.net core 6 ``BackgroundService`` that can be used by any .net 6 web app to subscribe to a postgres event stream. It defines an IApplicationHandler interface (a contract) that the web app must implement and provide via DI. The IApplicationHandler interface looks like this:

```
public interface IApplicationEventHandler
{
    Task OnNotification(List<PGEvent> newEvents);
    Task<int> GetLatestReceivedId();
} 
```

``OnNotification`` gets called by the PostgresEventSubscriber on every received notification with new events.
The consuming application must implement this method to be idempotent, see ApplicationEventHandler.cs in the webconsumer project for an example. How the consuming app does this and where (datastore) it keeps track of its received (or handled) events is up to the consuming app.

``GetLatestReceivedId`` gets called by the PostgresEventSubscriber every time it fetches new events from the eventsdatabase. This happens when the subscriber receives a notifcation on the channel, on application startup and when reconnecting after a connection loss.

## PREREQUISITES

* docker and docker-compose, for example via **Docker Desktop**

## RUN AND TEST SCENARIOS

all scenarios starts with you spinning up the system with: ```docker-compose up```  

### SCENARIO 1: Consumer is down for a while
1. stop the webconsumer when it has been running for a while (30 seconds perhaps..) by running: ```docker-compose stop web_consumer``` 
2. and then, after a while, start the webconsumer again by running: ```docker-compose start web_consumer```  

after step 1 the webconsumer will have missed some events, and will catch up on them when it starts again (step 2).  

See: **"Verify events received"** below for how to verify that the webconsumer has received all events.


### SCENARIO 2: Events database is down for a while

1. after a while stop the producer by running: ```docker-compose stop producer```
2. ... then stop the eventsdatabase by running: ```docker-compose stop db```
3. after a while start the eventsdatabase again by running: ```docker-compose start db```  
4. and finally start the producer again by running: ```docker-compose start producer```

in between steps 2 and 3 the webconsumer will loose its subscriptionConnection and will try to reconnect to the eventsdatabase. When the eventsdatabase is up again, the webconsumer will reconnect, fetch any missed events and continue to receive events. 

See: **"Verify events received"** below for how to verify that the webconsumer has received all events.

### Verify events received

You can verify this with **mongosh** and **psql** (or some GUI apps...)

**psql**  
1. connect to the database: ``PGPASSWORD=password psql -h localhost -p 5432 -U postgres -d eventsdb``
2. run the following query: ``select count(*) from events;`` (this will show the number of events produced by the producer)

**mongosh**  
1. connect to the database: ``mongosh``
2. authenticate: ``use admin`` and ``db.auth("mongo","password")``
3. use the consumer database: ``use consumer``
4. run the following query: ``db.receivedEvents.count()`` (this will show the number of events received by the webconsumer)


### services and ports
some services are mapped to localhost on different ports:
* postgres on port 5432
* pgadmin on port 5050
* mongo on port 27017
* webconsumer on port 1337 (example endpoint at http://localhost:1337/courses)

