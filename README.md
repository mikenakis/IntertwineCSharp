# Intertwine<br/><sup><sup><sub>Normalizing Interface Invocations</sup></sup></sub>

Michael Belivanakis (michael.gr) 2011

For more information about this project, see:
http://blog.michael.gr/2011/10/intertwine-normalizing-interface.html

The solution contains the following projects:

1. Intertwine - This is the central class library which accomplishes all the
   magic.

2. InterfaceEvents - Sample class library project which acts as a 
   proof-of-concept and demonstration of Intertwine.  It offers multicast
   events that invoke interfaces instead of delegates.

3. Test - Tests for Intertwine and InterfaceEvents

4. Benchmark - A console application performing a speed comparison between 
   Intertwine, hand-written code that achieves the same thing, Castle Dynamic
   Proxy with reflecting Untwiner, and LinFu Dynamic Proxy with reflecting
   Untwiner.
