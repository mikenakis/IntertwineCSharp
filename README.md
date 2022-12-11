# Intertwine<br/><sup><sup><sub>Normalizing Interface Invocations</sup></sup></sub>

Michael Belivanakis (michael.gr) 2011
- Revised October 2015
- Revised November 2022

For more information about this project, see:
http://blog.michael.gr/2011/10/intertwine-normalizing-interface.html

The solution contains the following projects:

1. Intertwine - This is the central class library which accomplishes all the magic.

2. InterfaceEvents - Sample class library project which acts as a proof-of-concept and demonstration of Intertwine.  It offers multicast events that invoke interfaces instead of delegates.

3. Test - Tests for Intertwine and InterfaceEvents

4. Benchmark - A console application performing a speed comparison between Intertwine, hand-written code that achieves the same thing, Castle Dynamic Proxy with reflecting Untwiner, and LinFu Dynamic Proxy with reflecting Untwiner.

## Abstract

A mechanism is proposed for converting (***entwining***) method call invocations of any interface to a general purpose single-method ***normal form***, and converting back (***untwining***) from the normal form to interface invocations, so that operations can be performed on the normal form in a way agnostic to the interface being invoked. The normal form is a *delegate* in C# or a *functional interface* in Java, realized as `object AnyCall( int selector, object[] parameters )`. A DotNet implementation is provided in C#, though the discussion also applies to Java.

## The Problem

When creating systems of nontrivial complexity it is often very useful to be able to perform operations on invocations taking place between subsystems without having any knowledge of the content of the invocations, analogously to how sorting and searching algorithms are agnostic to the meaning of the values that they sort or search.

Some examples of operations that can be performed on invocations are:

1. Counting how many times a subsystem invokes another.
2. Multicasting one invocation to multiple recipients.
3. Providing desynchronization of invocations between subsystems.
4. Remoting the invocations across machine boundaries.

In order to be able to perform operations on invocations, the invocations need to be expressed in some general purpose normal form. Traditionally, this normal form has been provided by means of ***messages*** and ***message-passing***. Once you have invocations expressed as messages, you can perform operations on the messages, such as counting, multicasting, routing, queuing, dequeuing, serializing, etc without any need to know what the messages contain.

### Why messages are bad

Unfortunately, communicating via messages imposes a considerable burden on the programmers, both on the sending and on the receiving end:

- Lots of custom message classes have to be written and maintained, shifting the focus of the programmers from the class hierarchy of their problem domain to the class hierarchy of their elaborate internal communication apparatus.

- For each invocation, a message class needs to be instantiated, filled, and submitted, requiring several lines of custom code, thus shifting the focus of the programmers from solving the problem at hand, to negotiating the trifling technicalities of inter-module communication.

- On the receiving end, each message must be examined, usually by means of an unwieldy `switch` statement, in order to determine what kind of message it is; then it needs to be cast to the correct message type; and then its contents have to be extracted before any useful action can be carried out. Again, this represents a significant amount of code which is not contributing towards the achievement of the goal of the software system; instead, its only purpose is to service the peculiarities of the message-passing mechanism.

- In order to reduce the number of different message classes that need to be written, programmers often reuse the same message class for different purposes, filling different parts of it according to each purpose, a habit which further complicates the code both at the sending and at the receiving end, and leads to an infinitude of bugs due to uninitialized message members or otherwise wrongly prepared messages.

Many programmers seem to regard messages as an end in and of themselves, oblivious to the fact that messages are simply a means for achieving a goal, (and a pretty handicapped means at that,) where the goal is to simply express invocations in a normal form. Manual creation, population, and submission of custom, hand-crafted message classes is an ineffective and counter-productive mechanism for expressing invocations in a normal form, which has traditionally been necessitated due to a lack of automated mechanisms for achieving the same thing.

### What is missing

The most straightforward, understandable, maintainable, self-documenting and practical paradigm for invocations, which facilitates problem-solving instead of hindering it, is interface method calls. Unfortunately, no standard, automatic mechanism exists for translating interface method calls to and from a normal form.

## The Solution

Thanks to modern reflecting, intermediate-code-based, boxing-unboxing, garbage-collected languages such as Java and C#, today we can easily inspect the anatomy of an interface at runtime, and we can generate code on the fly which provides marshalling and unmarshalling for it. So, today, we do have all that is necessary for converting between interface method call invocations and some convenient normal form. What follows is a description of such a mechanism, which I call ***Intertwine***. I have implemented Intertwine for DotNet in C#, but it should not be difficult for anyone who knows a thing or two about bytecode to write an implementation for the Java Virtual Machine.

The normal form used by intertwine for expressing interface method call invocations is called AnyCall, and it has the following signature:

```C#
delegate object AnyCall( int selector, object[] arguments );
```

This delegate essentially represents the fact that every conceivable interface method has:

1. A return parameter, the most general type of which is ‘object’;
1. A ***selector*** identifying the interface method which is being invoked;
1. A number of arguments, of the common denominator type ‘object’.

Note that value types such as primitives can be passed as objects, thanks to boxing and unboxing. Also note that various other features offered by C#, like properties, indexers and virtual events are nothing but syntactic sugar which is internally implemented using method calls, so they require no special handling.

So, for every interface that we want to be able to operate upon, we need two classes:

- An ***Entwiner*** (can also be thought of as a *normalizer* or *generalizer* or *multiplexer*) which:
  - Contains a reference to an AnyCall delegate that was given to it as a constructor parameter.
  - Implements our interface by forwarding every inbound interface method call to the AnyCall delegate after marshalling the arguments into an array.
- An ***Untwiner*** (can also be thought of as a *denormalizer* or *specializer* or *demultiplexer*) which:
  - Contains a reference to our interface that was given to it as a constructor parameter.
  - Implements the AnyCall delegate by switching on the method selector and placing an outbound call to the appropriate method of the interface, after the necessary unmarshalling.

### A hand-crafted solution

Let us take a look at an example of how we could implement an entwiner and an untwiner for an interface if we were to do it by hand. Let us consider the following interface:

```C#
public interface IFooable
{
    void Moo( int i );
    void Boo( string s, bool b );
}
```

And let us consider the following class implementing that interface:

```C#
public class FooImplementation: IFooable
{
    void IFooable.Moo( int i )
	    { Console.WriteLine( "i: " + i ); }
    void IFooable.Boo( string s, bool b )
	    { Console.WriteLine( "s: " + s + ", b: " + b ); }
}
```

And then let us consider the following class which invokes the interface:

```C#
public class Test
{
    public static void InvokeFoo( IFooable fooable )
    {
        fooable.Moo( 42 );
        fooable.Boo( "fubar!", true );
    }
}
````

Of course, the invoking method can be directly hooked up to an instance of the implementing class in a completely conventional way as follows:

```C#
public class Test
{
    public static void Run1()
    {
        IFooable fooable = new FooImplementation();
        InvokeFoo( fooable );
    }
}
```

Now, an entwiner for our IFooable interface could be hand-crafted as follows:

```C#
public class EntwinerForFooable: IFooable
{
    private readonly AnyCall AnyCall;
    public EntwinerForFooable( AnyCall anycall ) { AnyCall = anycall; }
    void IFooable.Moo( int i ) { AnyCall( 0, new object[]{ i } ); }
    void IFooable.Boo( string s, bool b ) { AnyCall( 1, new object[]{ s, b } ); }
}
```

Whereas an untwiner for IFooable could be hand-crafted as follows:

```C#
public class UntwinerForFooable
{
    public readonly IFooable Target;

    public UntwinerForFooable( IFooable target ) { Target = target; }

    public object AnyCall( int selector, object[] args )
    {
        switch( selector )
        {
            case 0: Target.Moo( (int)args[0] ); break;
            case 1: Target.Boo( (string)args[0], (bool)args[1] ); break;
            default: throw new System.InvalidOperationException();
        }
        return null;
    }
}
```

With the above classes, we can now write the following piece of awesomeness:

```C#
public class Test
{
    public static void Run2()
    {
        IFooable fooable = new FooImplementation();
        var untwiner = new UntwinerForFooable( fooable );
        var entwiner = new EntwinerForFooable( untwiner.AnyCall );
        InvokeFoo( entwiner );
    }
}
```

Note that `Run2()` has exactly the same end-result as `Run1()`, but there is a big difference in what goes on under the hood: all outbound interface method calls from the `InvokeFoo` function are now arriving at the entwiner, which converts them to AnyCall invocations and forwards them to the untwiner, which converts them back to `IFooable` calls and forwards them to our `FooImplementation` object. This means that if we wanted to, we could interject a chain of objects between the entwiner and the untwiner, each one of these objects implementing an AnyCall delegate and forwarding calls to another AnyCall delegate, thus enabling us to perform any conceivable operation upon those invocations without having any built-in knowledge of the `IFooable` interface.

As the complexity of the interface increases, and as additional subtleties such as parameters passed with `ref` or `out` come into the picture, coding entwiners and untwiners by hand can quickly start becoming a very tedious and error-prone business. It is possible to write a general-purpose untwiner that does its job using reflection, but reflection is slow, so the result is going to suffer performance-wise. For the sake of completeness, here is a possible implementation for a general-purpose reflecting untwiner using reflection:

```C#
public class ReflectingUntwiner //WARNING: SLOW AS MOLASSES
{
    private readonly object Target;
    private readonly System.Reflection.MethodInfo[] Methodinfos;
    
    public ReflectingUntwiner( Type twinee, object target )
    {
        Target = target;
        Methodinfos = twinee.GetMethods( BindingFlags.Public |
        BindingFlags.NonPublic | BindingFlags.Instance );
    }

    public override object AnyCall( int selector, object[] arguments )
    {
        return Methodinfos[selector].Invoke( Target, arguments );
    }
}
```

Note: untwiner creation could be optimized by caching the `MethodInfo`s of frequently used types, but that’s not the problem; the real bottleneck is the `MethodInfo.Invoke()` call, which uses reflection on each invocation in order to do its job. If you put a breakpoint on the target and examine the stack, you will see that between the `MethodInfo.Invoke()` frame and the target frame there will be a managed-to-native transition and a native-to-managed transition. That is to be avoided at all costs.

Also note: it is impossible to write a reflecting entwiner.

### Automating it with Intertwine

The Intertwine system will automatically generate for us at runtime a pair of optimally-performing entwiner and untwiner classes for any interface.

The following method of the `Intertwine.Factory` class creates an entwiner:

```C#
public static T NewEntwiner<T>( AnyCall anycall );
```

For `T` we give the type of our interface, and for `anycall` we give a delegate of ours that will be receiving calls. This method returns a reference to an implementation of our interface, provided by an `Entwiner`-derived class that has been dynamically generated specifically for our interface, and instantiated to work with the given `AnyCall` instance. For every call received through a method of our interface, this special entwiner will be marshalling the arguments and forwarding the call to our `AnyCall` delegate.

The following method of the `Intertwine.Factory` class creates an untwiner:

```C#
public static AnyCall NewUntwiner<T>( T target );
```

For `target` we give an implementation of our interface, and what we get is a reference to an `AnyCall` delegate implemented by an `Untwiner`-derived class that was dynamically generated specifically for our interface, and instantiated to work with the given target instance. For every call received through the `AnyCall` delegate, this special untwiner will be unmarshalling the arguments and forwarding the call to the appropriate method of our target interface.

So, with the dynamically generated entwiners and untwiners we can now do the following epicness:

```C#
public class Test
{
	public static void Run3()
	{
		IFooable fooable = new FooImplementation();
		AnyCall untwiner = Intertwine.Factory.NewUntwiner<IFooable>( fooable );
		IFooable entwiner = Intertwine.Factory.NewEntwiner<IFooable>( untwiner );
		InvokeFoo( entwiner );
	}
}
```

The actual implementation of `Intertwine.Factory` is pretty straightforward, so there is not much to talk about. As one might expect, the generated types are cached. A static factory method is generated with each generated type, for instantiating the type, so as to avoid having to call `Activator.CreateInstance()`, because that method uses reflection. The static factory method is invoked using `Delegate.Invoke()`, which does not use reflection. You will find the code-generating code choke-full of comments, explaining exactly what each emitted opcode does.

## Appendix 1: A note about Dynamic Proxies

Readers who are not already familiar with ***dynamic proxies*** such as *Castle’s* or *LinFu* can skip this section, as it might only confuse them. Readers who are familiar with them and are wondering whether I am reinventing that particular wheel can read this section to see why I am not.

Dynamic proxies are meant to address a problem which is different from the one addressed by Intertwine, and as far as I know they cannot be used (or at least they cannot be used efficiently) to achieve what Intertwine achieves. A dynamic proxy is meant to encapsulate an object; Intertwining is meant to act as an intermediary between two interfaces. These two roles are functionally distinct. You can certainly use a dynamic proxy to create an entwiner for an interface, but you cannot use it to create an untwiner. As far as I can tell about Castle, it does internally generate the code necessary to invoke a target interface
without reflection, (which is what an untwiner also does,) but it does not make that code available through any means other than its interceptor mechanism, so it is of no use to us if we already have all the information about a method call in normal form, and all we want to do is to dispatch it to the right method of an interface.

I may be wrong, but the only way I found to use dynamic proxies to emulate the functionality of Intertwine was with the help of ReflectingUntwiner for the part of the job that the dynamic proxies will not do for me. Thus, I ran some benchmarks, and it came to no surprise that Intertwine was running circles around them: they were being dragged behind by the huge performance penalty incurred by reflection.

Even without the performance penalty incurred by using `ReflectingUntwiner`, the dynamic proxies are doomed to be slower than Intertwine, because for every invocation they do a lot more than what Intertwine does: Intertwine simply marshals and unmarshals arguments; dynamic proxies also collect information about the caller, the callee, the method, each argument of the method, etc. and they make all that information available to the interceptor, in case they are needed.

## Appendix 2: An example: Interface multicasts (events)

If you are still with me you may be thinking that it is about time for a demonstration. What follows is not just an example, but actually a complete and useful application of intertwine which you may be able to start utilizing in your projects right away.

The C# language has built-in support for multicasts (events) but only delegates can be used as event observers. There are many cases, however, where interfaces would be more suitable. Java does not even have built-in support for multicasts, so programmers generally have to write their own, using single-method (functional) interfaces. In either language, if you want to achieve multicasting on multi-method interfaces, you have to rewrite the multicasting code for every single method of every single interface.

Consider the following interface:

```C#
interface ITableNotification
{
    void RowInserted( Fields fields );
    void RowDeleted( Key key );
    void RowUpdated( Key key, Fields fields );
}
```

And consider the following hypothetical way of using it:

```C#
    event ITableNotification tableNotificationEvent;
    tableNotificationEvent += my_observer;
    tableNotificationEvent.RowUpdated( key, fields );
```

The above is not actually possible with C# today, but with Intertwine the next best thing is actually possible:

```C#
	var tableNotificationEventManager = new InterfaceEventManager<ITableNotifcation>();
	tableNotificationEventManager.Source.RegisterObserver( my_observer );
	tableNotificationEventManager.Trigger.RowUpdated( key, fields );
```

This approach is self-explanatory, and the amount of code you have to write in order to use it is optimal; you do not need to deal with anything more than what is necessary, and if you ever add a notification, it will be a new interface method, so all existing implementations of that interface will automatically be flagged by the compiler as incomplete. With the help of Intertwine, this event manager is implemented in just 150 lines of code, including extensive comments.

## Appendix 3: Things to fix

- Make the `Intertwine.Factory` class instantiatable instead of a singleton, get rid of the `IsCaching` constant, and make `IsSaving` a parameter.
- Add support for a few missing types, such as `Decimal`, or make it handle all value types in a uniform way.
- Add support for creating only the entwiner class or only the untwiner class if the other one is never needed.
- Use objects for keys (perhaps `Method` objects?) instead of integer selectors.
- See if `RegisterObserver` and `UnregisterObserver` can be implemented as `+=` and `-=` operators.

■
