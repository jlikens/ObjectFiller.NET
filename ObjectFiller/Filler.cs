// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Filler.cs" company="Tynamix">
//   � 2015 by Roman K�hler
// </copyright>
// <summary>
//   The ObjectFiller.NET fills the public properties of your .NET object
//   with random data
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Tynamix.ObjectFiller
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// The ObjectFiller.NET fills the public properties of your .NET object
    /// with random data
    /// </summary>
    /// <typeparam name="T">
    /// Target dictionaryType of the object to fill
    /// </typeparam>
    public class Filler<T>
        where T : class
    {
        #region Fields

        /// <summary>
        /// The setup manager contains the setup per dictionaryType
        /// </summary>
        private readonly SetupManager setupManager;

        #endregion Fields

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Filler{T}"/> class.
        /// </summary>
        public Filler()
        {
            this.setupManager = new SetupManager();
        }

        #endregion Constructors and Destructors

        #region Public Methods and Operators

        /// <summary>
        /// Creates your filled object. Call this after you finished your setup with the FluentAPI and if you want
        /// to create a new object. If you want to use a existing instance use the <see cref="Fill(T)"/> method.
        /// </summary>
        /// <returns>
        /// A created and filled instance of dictionaryType <see cref="T"/>
        /// </returns>
        public T Create()
        {
            T objectToFill;
            var hashStack = new HashStack<Type>();
            if (!TypeIsClrType(typeof(T)))
            {
                objectToFill = (T)this.CreateInstanceOfType(typeof(T), this.setupManager.GetFor<T>(), hashStack);
                this.Fill(objectToFill);
            }
            else
            {
                objectToFill = (T)this.CreateAndFillObject(typeof(T), this.setupManager.GetFor<T>(), hashStack);
            }

            return objectToFill;
        }

        /// <summary>
        /// Creates multiple filled objects. Call this after you finished your setup with the FluentAPI and if you want
        /// to create several new objects. If you want to use a existing instance use the <see cref="Fill(T)"/> method.
        /// </summary>
        /// <param name="count">
        /// Count of instances to create
        /// </param>
        /// <returns>
        /// <see cref="IEnumerable{T}"/> with created and filled instances of dictionaryType <see cref="T"/>
        /// </returns>
        public IEnumerable<T> Create(int count)
        {
            IList<T> items = new List<T>();
            var typeStack = new HashStack<Type>();
            Type targetType = typeof(T);
            for (int n = 0; n < count; n++)
            {
                T objectToFill;
                if (!TypeIsClrType(targetType))
                {
                    objectToFill = (T)this.CreateInstanceOfType(targetType, this.setupManager.GetFor<T>(), typeStack);
                    this.Fill(objectToFill);
                }
                else
                {
                    objectToFill = (T)this.CreateAndFillObject(typeof(T), this.setupManager.GetFor<T>(), typeStack);
                }

                items.Add(objectToFill);
            }

            return items;
        }

        /// <summary>
        /// Fills your object instance. Call this after you finished your setup with the FluentAPI
        /// </summary>
        /// <param name="instanceToFill">
        /// The instance To fill.
        /// </param>
        /// <returns>
        /// The filled instance of dictionaryType <see cref="T"/>.
        /// </returns>
        public T Fill(T instanceToFill)
        {
            this.FillInternal(instanceToFill);

            return instanceToFill;
        }

        /// <summary>
        /// Call this to start the setup for the <see cref="Filler{T}"/>
        /// </summary>
        /// <returns>Fluent API setup</returns>
        public FluentFillerApi<T> Setup()
        {
            return this.Setup(null);
        }

        /// <summary>
        /// Call this to start the setup for the <see cref="Filler{T}"/> and use a setup which you created
        /// before with the <see cref="IFluentApi{TTargetObject,TTargetType}"/>
        /// </summary>
        /// <param name="fillerSetupToUse">
        /// FillerSetup to use
        /// </param>
        /// <returns>
        /// Fluent API Setup
        /// </returns>
        public FluentFillerApi<T> Setup(FillerSetup fillerSetupToUse)
        {
            if (fillerSetupToUse != null)
            {
                this.setupManager.FillerSetup = fillerSetupToUse;
            }

            return new FluentFillerApi<T>(this.setupManager);
        }

        #endregion Public Methods and Operators

        #region Methods

        /// <summary>
        /// Checks if the dictionary parameter types are valid to use with object filler
        /// </summary>
        /// <param name="dictionaryType">
        /// The type of the dictionary.
        /// </param>
        /// <param name="currentSetupItem">
        /// The current setup item.
        /// </param>
        /// <returns>
        /// True if the dictionary parameter types are valid for use with object filler
        /// </returns>
        private static bool DictionaryParamTypesAreValid(Type dictionaryType, FillerSetupItem currentSetupItem)
        {
            if (!TypeIsDictionary(dictionaryType))
            {
                return false;
            }

            Type keyType = dictionaryType.GetGenericArguments()[0];
            Type valueType = dictionaryType.GetGenericArguments()[1];

            return TypeIsValidForObjectFiller(keyType, currentSetupItem)
                   && TypeIsValidForObjectFiller(valueType, currentSetupItem);
        }

        /// <summary>
        /// Creates a default value for the given <see cref="propertyType"/>
        /// </summary>
        /// <param name="propertyType">
        /// The property dictionaryType.
        /// </param>
        /// <returns>
        /// Default value for the given <see cref="propertyType"/>
        /// </returns>
        private static object GetDefaultValueOfType(Type propertyType)
        {
            if (propertyType.IsValueType)
            {
                return Activator.CreateInstance(propertyType);
            }

            return null;
        }

        /// <summary>
        /// Checks if there is a random function for the given <see cref="type"/>
        /// </summary>
        /// <param name="type">
        /// The dictionaryType.
        /// </param>
        /// <param name="currentSetupItem">
        /// The current setup item.
        /// </param>
        /// <returns>
        /// True if there is a random function in the <see cref="currentSetupItem"/> for the given <see cref="type"/>
        /// </returns>
        private static bool HasTypeARandomFunc(Type type, FillerSetupItem currentSetupItem)
        {
            return currentSetupItem.TypeToRandomFunc.ContainsKey(type);
        }

        /// <summary>
        /// Checks if the list parameter type are valid to use with object filler
        /// </summary>
        /// <param name="listType">
        /// The type of the list.
        /// </param>
        /// <param name="currentSetupItem">
        /// The current setup item.
        /// </param>
        /// <returns>
        /// True if the list parameter types are valid for use with object filler
        /// </returns>
        private static bool ListParamTypeIsValid(Type listType, FillerSetupItem currentSetupItem)
        {
            if (!TypeIsList(listType))
            {
                return false;
            }

            Type genType = listType.GetGenericArguments()[0];

            return TypeIsValidForObjectFiller(genType, currentSetupItem);
        }

        /// <summary>
        /// Checks if the given <see cref="type"/> is a dictionary
        /// </summary>
        /// <param name="type">
        /// The type to check
        /// </param>
        /// <returns>
        /// True if the target <see cref="type"/>  is a dictionary
        /// </returns>
        private static bool TypeIsDictionary(Type type)
        {
            return type.GetInterfaces().Any(x => x == typeof(IDictionary));
        }

        /// <summary>
        /// Checks if the given <see cref="type"/> is a list
        /// </summary>
        /// <param name="type">
        /// The type to check
        /// </param>
        /// <returns>
        /// True if the target <see cref="type"/>  is a list
        /// </returns>
        private static bool TypeIsList(Type type)
        {
            return !type.IsArray && type.IsGenericType && type.GetGenericArguments().Length != 0
                   && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                       || type.GetInterfaces().Any(x => x == typeof(IEnumerable)));
        }

        /// <summary>
        /// Checks if the given <see cref="type"/> is a plain old class object
        /// </summary>
        /// <param name="type">
        /// The type to check
        /// </param>
        /// <returns>
        /// True if the target <see cref="type"/> is a plain old class object
        /// </returns>
        private static bool TypeIsPoco(Type type)
        {
            return !type.IsValueType && !type.IsArray && type.IsClass && type.GetProperties().Length > 0
                   && (type.Namespace == null
                       || (!type.Namespace.StartsWith("System") && !type.Namespace.StartsWith("Microsoft")));
        }

        /// <summary>
        /// Check if the given type is a type from the common language library
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if the given type is a type from the common language library</returns>
        private static bool TypeIsClrType(Type type)
        {
            return (type.Namespace != null && (type.Namespace.StartsWith("System") || type.Namespace.StartsWith("Microsoft")))
                    || type.Module.ScopeName == "CommonLanguageRuntimeLibrary";
        }

        /// <summary>
        /// Checks if the given <see cref="type"/> can be used by the object filler
        /// </summary>
        /// <param name="type">
        /// The dictionaryType which will be checked
        /// </param>
        /// <param name="currentSetupItem">
        /// The current setup item.
        /// </param>
        /// <returns>
        /// True when the <see cref="type"/> can be used with object filler
        /// </returns>
        private static bool TypeIsValidForObjectFiller(Type type, FillerSetupItem currentSetupItem)
        {
            var result = HasTypeARandomFunc(type, currentSetupItem)
                   || (TypeIsList(type) && ListParamTypeIsValid(type, currentSetupItem))
                   || (TypeIsDictionary(type) && DictionaryParamTypesAreValid(type, currentSetupItem))
                   || TypeIsPoco(type)
                   || TypeIsEnum(type)
                   || (type.IsInterface && currentSetupItem.InterfaceToImplementation.ContainsKey(type)
                       || currentSetupItem.InterfaceMocker != null);

            return result;
        }

        /// <summary>
        /// Checks if the given dictionaryType was already been used in the object hierarchy
        /// </summary>
        /// <param name="targetType">
        /// The target dictionaryType.
        /// </param>
        /// <param name="typeTracker">
        /// The dictionaryType tracker to find circular dependencies
        /// </param>
        /// <param name="currentSetupItem">
        /// The current setup item.
        /// </param>
        /// <returns>
        /// True if there is a circular dependency
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Throws exception if a circular dependency exists and the setup is set to throw exception on circular dependency
        /// </exception>
        private bool CheckForCircularReference(
            Type targetType,
            HashStack<Type> typeTracker,
            FillerSetupItem currentSetupItem)
        {
            if (typeTracker != null)
            {
                if (typeTracker.Contains(targetType))
                {
                    if (currentSetupItem.ThrowExceptionOnCircularReference)
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "The dictionaryType {0} was already encountered before, which probably means you have a circular reference in your model. Either ignore the properties which cause this or specify explicit creation rules for them which do not rely on types.",
                                targetType.Name));
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a <see cref="property"/> exists in the given list of <see cref="properties"/>
        /// </summary>
        /// <param name="properties">
        /// Source properties where to check if the <see cref="property"/> is contained
        /// </param>
        /// <param name="property">
        /// The property which will be checked
        /// </param>
        /// <returns>
        /// True if the <see cref="property"/> is in the list of <see cref="properties"/>
        /// </returns>
        private bool ContainsProperty(IEnumerable<PropertyInfo> properties, PropertyInfo property)
        {
            return this.GetPropertyFromProperties(properties, property).Any();
        }

        /// <summary>
        /// Creates a object of the target <see cref="type"/> and fills it up with data according to the given <see cref="currentSetupItem"/>
        /// </summary>
        /// <param name="type">
        /// The target dictionaryType to create and fill
        /// </param>
        /// <param name="currentSetupItem">
        /// The current setup item.
        /// </param>
        /// <param name="typeTracker">
        /// The dictionaryType tracker to find circular dependencies
        /// </param>
        /// <returns>
        /// The created and filled object of the given <see cref="type"/>
        /// </returns>
        private object CreateAndFillObject(
            Type type,
            FillerSetupItem currentSetupItem,
            HashStack<Type> typeTracker = null)
        {
            if (HasTypeARandomFunc(type, currentSetupItem))
            {
                return this.GetRandomValue(type, currentSetupItem);
            }

            if (TypeIsDictionary(type))
            {
                IDictionary dictionary = this.GetFilledDictionary(type, currentSetupItem, typeTracker);

                return dictionary;
            }

            if (TypeIsList(type))
            {
                IList list = this.GetFilledList(type, currentSetupItem, typeTracker);
                return list;
            }

            if (type.IsInterface || type.IsAbstract)
            {
                return this.CreateInstanceOfInterfaceOrAbstractClass(type, currentSetupItem, typeTracker);
            }

            if (TypeIsEnum(type))
            {
                return this.GetRandomEnumValue(type);
            }

            if (TypeIsPoco(type))
            {
                return this.GetFilledPoco(type, currentSetupItem, typeTracker);
            }

            object newValue = this.GetRandomValue(type, currentSetupItem);
            return newValue;
        }

        /// <summary>
        /// Creates a instance of an interface or abstract class. Like an IoC-Framework
        /// </summary>
        /// <param name="interfaceType">
        /// The dictionaryType of interface or abstract class
        /// </param>
        /// <param name="setupItem">
        /// The setup item.
        /// </param>
        /// <param name="typeTracker">
        /// The dictionaryType tracker to find circular dependencies
        /// </param>
        /// <returns>
        /// The created and filled instance of the <see cref="interfaceType"/>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Throws Exception if no dictionaryType was registered for the given <see cref="interfaceType"/>
        /// </exception>
        private object CreateInstanceOfInterfaceOrAbstractClass(
            Type interfaceType,
            FillerSetupItem setupItem,
            HashStack<Type> typeTracker)
        {
            object result;
            if (setupItem.TypeToRandomFunc.ContainsKey(interfaceType))
            {
                return setupItem.TypeToRandomFunc[interfaceType]();
            }

            if (setupItem.InterfaceToImplementation.ContainsKey(interfaceType))
            {
                Type implType = setupItem.InterfaceToImplementation[interfaceType];
                result = this.CreateInstanceOfType(implType, setupItem, typeTracker);
            }
            else
            {
                if (setupItem.InterfaceMocker == null)
                {
                    string message =
                        string.Format(
                            "ObjectFiller Interface mocker missing and dictionaryType [{0}] not registered",
                            interfaceType.Name);
                    throw new InvalidOperationException(message);
                }

                MethodInfo method = setupItem.InterfaceMocker.GetType().GetMethod("Create");
                MethodInfo genericMethod = method.MakeGenericMethod(new[] { interfaceType });
                result = genericMethod.Invoke(setupItem.InterfaceMocker, null);
            }

            this.FillInternal(result, typeTracker);
            return result;
        }

        /// <summary>
        /// Creates a instance of the given <see cref="type"/>
        /// </summary>
        /// <param name="type">
        /// The dictionaryType to create
        /// </param>
        /// <param name="currentSetupItem">
        /// The setup for the current object dictionaryType
        /// </param>
        /// <param name="typeTracker">
        /// The dictionaryType tracker to find circular dependencies
        /// </param>
        /// <returns>
        /// Created instance of the given <see cref="Type"/>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Throws exception if the constructor could not be created by filler setup
        /// </exception>
        private object CreateInstanceOfType(Type type, FillerSetupItem currentSetupItem, HashStack<Type> typeTracker)
        {
            var constructorArgs = new List<object>();

            if (type.GetConstructors().All(ctor => ctor.GetParameters().Length != 0))
            {
                IEnumerable<ConstructorInfo> ctorInfos;
                if ((ctorInfos = type.GetConstructors().Where(ctr => ctr.GetParameters().Length != 0)).Count() != 0)
                {
                    foreach (ConstructorInfo ctorInfo in ctorInfos.OrderBy(x => x.GetParameters().Length))
                    {
                        Type[] paramTypes = ctorInfo.GetParameters().Select(p => p.ParameterType).ToArray();

                        if (paramTypes.All(t => TypeIsValidForObjectFiller(t, currentSetupItem)))
                        {
                            foreach (Type paramType in paramTypes)
                            {
                                constructorArgs.Add(this.CreateAndFillObject(paramType, currentSetupItem, typeTracker));
                            }

                            break;
                        }
                    }

                    if (constructorArgs.Count == 0)
                    {
                        var message = "Could not found a constructor for dictionaryType [" + type.Name
                                      + "] where the parameters can be filled with the current objectfiller setup";
                        throw new InvalidOperationException(message);
                    }
                }
            }

            object result = Activator.CreateInstance(type, constructorArgs.ToArray());
            return result;
        }

        /// <summary>
        /// Fills the given <see cref="objectToFill"/> with random data
        /// </summary>
        /// <param name="objectToFill">
        /// The object to fill.
        /// </param>
        /// <param name="typeTracker">
        /// The dictionaryType tracker to find circular dependencies
        /// </param>
        private void FillInternal(object objectToFill, HashStack<Type> typeTracker = null)
        {
            var currentSetup = this.setupManager.GetFor(objectToFill.GetType());
            var targetType = objectToFill.GetType();

            typeTracker = typeTracker ?? new HashStack<Type>();

            if (currentSetup.TypeToRandomFunc.ContainsKey(targetType))
            {
                objectToFill = currentSetup.TypeToRandomFunc[targetType]();
                return;
            }

            var properties =
                targetType.GetProperties().Where(prop => this.GetSetMethodOnDeclaringType(prop) != null).ToArray();

            if (properties.Length == 0)
            {
                return;
            }

            Queue<PropertyInfo> orderedProperties = this.OrderPropertiers(currentSetup, properties);
            while (orderedProperties.Count != 0)
            {
                PropertyInfo property = orderedProperties.Dequeue();

                if (currentSetup.TypesToIgnore.Contains(property.PropertyType))
                {
                    continue;
                }

                if (this.IgnoreProperty(property, currentSetup))
                {
                    continue;
                }

                if (this.ContainsProperty(currentSetup.PropertyToRandomFunc.Keys, property))
                {
                    PropertyInfo propertyInfo =
                        this.GetPropertyFromProperties(currentSetup.PropertyToRandomFunc.Keys, property).Single();
                    this.SetPropertyValue(property, objectToFill, currentSetup.PropertyToRandomFunc[propertyInfo]());
                    continue;
                }

                object filledObject = this.CreateAndFillObject(property.PropertyType, currentSetup, typeTracker);

                this.SetPropertyValue(property, objectToFill, filledObject);
            }
        }

        /// <summary>
        /// Creates and fills a dictionary of the target <see cref="propertyType"/>
        /// </summary>
        /// <param name="propertyType">
        /// The dictionaryType of the dictionary
        /// </param>
        /// <param name="currentSetupItem">
        /// The current setup item.
        /// </param>
        /// <param name="typeTracker">
        /// The dictionaryType tracker to find circular dependencies
        /// </param>
        /// <returns>
        /// A created and filled dictionary
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Throws exception if the setup was made in a way that the keys of the dictionary are always the same
        /// </exception>
        private IDictionary GetFilledDictionary(
            Type propertyType,
            FillerSetupItem currentSetupItem,
            HashStack<Type> typeTracker)
        {
            IDictionary dictionary = (IDictionary)Activator.CreateInstance(propertyType);
            Type keyType = propertyType.GetGenericArguments()[0];
            Type valueType = propertyType.GetGenericArguments()[1];
            List<object> dictionaryKeys = GenerateDictionaryKeys(keyType, currentSetupItem, typeTracker);

            foreach (var keyObject in dictionaryKeys)
            {
                if (dictionary.Contains(keyObject))
                {
                    string message =
                        string.Format(
                            "Generating Keyvalue failed because it generates always the same data for dictionaryType [{0}]. Please check your setup.",
                            keyType);
                    throw new ArgumentException(message);
                }

                object valueObject = this.CreateAndFillObject(valueType, currentSetupItem, typeTracker);
                dictionary.Add(keyObject, valueObject);
            }

            return dictionary;
        }

        /// <summary>
        /// Generates keys for a randomized dictionary.  If <paramref name="keyType"/> is an enumeration, special
        /// handling is taken to ensure that duplicates aren't created.
        /// </summary>
        /// <param name="keyType">The dictionary key type</param>
        /// <param name="currentSetupItem">The current setup item</param>
        /// <param name="typeTracker">The dictionaryType tracker to find circular dependencies</param>
        /// <returns></returns>
        private List<object> GenerateDictionaryKeys(Type keyType, FillerSetupItem currentSetupItem, HashStack<Type> typeTracker)
        {
            List<object> keys = new List<object>();

            if (keyType.IsEnum)
            {
                // TODO: The result of this could be cached for performance if desired
                var enumValues = Enum.GetValues(keyType).Cast<object>().ToList();

                int maxDictionaryItems = Random.Next(
                    Math.Min(enumValues.Count, currentSetupItem.DictionaryKeyMinCount),
                    Math.Min(enumValues.Count, currentSetupItem.DictionaryKeyMaxCount));

                for (int i = 0; i < maxDictionaryItems; i++)
                {
                    int randomIndex = Random.Next(enumValues.Count);
                    var key = enumValues[randomIndex];
                    keys.Add(key);
                    enumValues.Remove(key);
                }
            }
            else
            {
                int maxDictionaryItems = Random.Next(
                    currentSetupItem.DictionaryKeyMinCount,
                    currentSetupItem.DictionaryKeyMaxCount);

                for (int i = 0; i < maxDictionaryItems; i++)
                {
                    keys.Add(this.CreateAndFillObject(keyType, currentSetupItem, typeTracker));
                }
            }

            return keys;
        }

        /// <summary>
        /// Creates and fills a list of the given <see cref="propertyType"/>
        /// </summary>
        /// <param name="propertyType">
        /// Type of the list
        /// </param>
        /// <param name="currentSetupItem">
        /// The current setup item.
        /// </param>
        /// <param name="typeTracker">
        /// The dictionaryType tracker to find circular dependencies
        /// </param>
        /// <returns>
        /// Created and filled list of the given <see cref="propertyType"/>
        /// </returns>
        private IList GetFilledList(Type propertyType, FillerSetupItem currentSetupItem, HashStack<Type> typeTracker)
        {
            Type genType = propertyType.GetGenericArguments()[0];

            if (this.CheckForCircularReference(genType, typeTracker, currentSetupItem))
            {
                return null;
            }

            IList list;
            if (!propertyType.IsInterface
                && propertyType.GetInterfaces().Any(x => x.GetGenericTypeDefinition() == typeof(ICollection<>)))
            {
                list = (IList)Activator.CreateInstance(propertyType);
            }
            else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                     || propertyType.GetInterfaces().Any(x => x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                Type openListType = typeof(List<>);
                Type genericListType = openListType.MakeGenericType(genType);
                list = (IList)Activator.CreateInstance(genericListType);
            }
            else
            {
                list = (IList)Activator.CreateInstance(propertyType);
            }

            int maxListItems = Random.Next(currentSetupItem.ListMinCount, currentSetupItem.ListMaxCount);
            for (int i = 0; i < maxListItems; i++)
            {
                object listObject = this.CreateAndFillObject(genType, currentSetupItem, typeTracker);
                list.Add(listObject);
            }

            return list;
        }

        /// <summary>
        /// Creates and fills a POCO class
        /// </summary>
        /// <param name="type">
        /// The target dictionaryType.
        /// </param>
        /// <param name="currentSetupItem">
        /// The current setup item.
        /// </param>
        /// <param name="typeTracker">
        /// The dictionaryType tracker to find circular dependencies
        /// </param>
        /// <returns>
        /// The created and filled POCO class
        /// </returns>
        private object GetFilledPoco(Type type, FillerSetupItem currentSetupItem, HashStack<Type> typeTracker)
        {
            if (this.CheckForCircularReference(type, typeTracker, currentSetupItem))
            {
                return GetDefaultValueOfType(type);
            }

            typeTracker.Push(type);

            object result = this.CreateInstanceOfType(type, currentSetupItem, typeTracker);

            this.FillInternal(result, typeTracker);

            typeTracker.Pop();

            return result;
        }

        /// <summary>
        /// Selects the given <see cref="property"/> from the given list of <see cref="properties"/>
        /// </summary>
        /// <param name="properties">
        /// All properties where the target <see cref="property"/> will be searched in
        /// </param>
        /// <param name="property">
        /// The target property.
        /// </param>
        /// <returns>
        /// All properties from <see cref="properties"/> which are the same as the target <see cref="property"/>
        /// </returns>
        private IEnumerable<PropertyInfo> GetPropertyFromProperties(
            IEnumerable<PropertyInfo> properties,
            PropertyInfo property)
        {
            return properties.Where(x => x.MetadataToken == property.MetadataToken && x.Module.Equals(property.Module));
        }

        /// <summary>
        /// Gets a random value for an enumeration
        /// </summary>
        /// <param name="type">
        /// Type of the enumeration
        /// </param>
        /// <returns>
        /// A default value for an enumeration
        /// </returns>
        private object GetRandomEnumValue(Type type)
        {
            // performance: Enum.GetValues() is slow due to reflection, should cache it
            Array values = Enum.GetValues(type);
            if (values.Length > 0)
            {
                int index = Random.Next() % values.Length;
                return values.GetValue(index);
            }

            return 0;
        }

        /// <summary>
        /// Gets a random value of the given <see cref="propertyType"/>
        /// </summary>
        /// <param name="propertyType">
        /// The property dictionaryType.
        /// </param>
        /// <param name="setupItem">
        /// The setup item.
        /// </param>
        /// <returns>
        /// A random value of the given <see cref="propertyType"/>
        /// </returns>
        /// <exception cref="TypeInitializationException">
        /// Throws exception if object filler was not able to create random data
        /// </exception>
        private object GetRandomValue(Type propertyType, FillerSetupItem setupItem)
        {
            if (setupItem.TypeToRandomFunc.ContainsKey(propertyType))
            {
                return setupItem.TypeToRandomFunc[propertyType]();
            }

            if (setupItem.IgnoreAllUnknownTypes)
            {
                return GetDefaultValueOfType(propertyType);
            }

            string message = "The dictionaryType [" + propertyType.Name + "] was not registered in the randomizer.";
            throw new TypeInitializationException(propertyType.FullName, new Exception(message));
        }

        /// <summary>
        /// Gets the setter of a <see cref="propInfo"/>
        /// </summary>
        /// <param name="propInfo">
        /// The <see cref="PropertyInfo"/> for which the setter method will be found
        /// </param>
        /// <returns>
        /// The setter of the property as <see cref="MethodInfo"/>
        /// </returns>
        private MethodInfo GetSetMethodOnDeclaringType(PropertyInfo propInfo)
        {
            var methodInfo = propInfo.GetSetMethod(true);

            if (propInfo.DeclaringType != null)
            {
                return methodInfo ?? propInfo.DeclaringType.GetProperty(propInfo.Name).GetSetMethod(true);
            }

            return null;
        }

        /// <summary>
        /// Checks if a property is ignored by the <see cref="currentSetupItem"/>
        /// </summary>
        /// <param name="property">
        /// The property to check for ignorance
        /// </param>
        /// <param name="currentSetupItem">
        /// The current setup item.
        /// </param>
        /// <returns>
        /// True if the <see cref="property"/> should be ignored
        /// </returns>
        private bool IgnoreProperty(PropertyInfo property, FillerSetupItem currentSetupItem)
        {
            return this.ContainsProperty(currentSetupItem.PropertiesToIgnore, property);
        }

        /// <summary>
        /// Sorts the properties like the <see cref="currentSetupItem"/> wants to have it
        /// </summary>
        /// <param name="currentSetupItem">
        /// The current setup item.
        /// </param>
        /// <param name="properties">
        /// The properties to sort
        /// </param>
        /// <returns>
        /// Sorted properties as a queue
        /// </returns>
        private Queue<PropertyInfo> OrderPropertiers(FillerSetupItem currentSetupItem, PropertyInfo[] properties)
        {
            var propertyQueue = new Queue<PropertyInfo>();
            var firstProperties =
                currentSetupItem.PropertyOrder.Where(
                    x => x.Value == At.TheBegin && this.ContainsProperty(properties, x.Key)).Select(x => x.Key).ToList();

            var lastProperties =
                currentSetupItem.PropertyOrder.Where(
                    x => x.Value == At.TheEnd && this.ContainsProperty(properties, x.Key)).Select(x => x.Key).ToList();

            var propertiesWithoutOrder =
                properties.Where(x => !this.ContainsProperty(currentSetupItem.PropertyOrder.Keys, x)).ToList();

            firstProperties.ForEach(propertyQueue.Enqueue);
            propertiesWithoutOrder.ForEach(propertyQueue.Enqueue);
            lastProperties.ForEach(propertyQueue.Enqueue);

            return propertyQueue;
        }

        /// <summary>
        /// Sets the given <see cref="value"/> on the given <see cref="property"/> for the given <see cref="objectToFill"/>
        /// </summary>
        /// <param name="property">
        /// The property to set
        /// </param>
        /// <param name="objectToFill">
        /// The object to fill.
        /// </param>
        /// <param name="value">
        /// The value for the <see cref="property"/>
        /// </param>
        private void SetPropertyValue(PropertyInfo property, object objectToFill, object value)
        {
            if (property.CanWrite)
            {
                property.SetValue(objectToFill, value, null);
            }
            else
            {
                MethodInfo m = this.GetSetMethodOnDeclaringType(property);
                m.Invoke(objectToFill, new[] { value });
            }
        }

        /// <summary>
        /// Checks if the given <see cref="type"/> is a enumeration
        /// </summary>
        /// <param name="type">
        /// The type to check
        /// </param>
        /// <returns>
        /// True if the target <see cref="type"/>  is a enumeration
        /// </returns>
        private static bool TypeIsEnum(Type type)
        {
            return type.IsEnum;
        }

        #endregion Methods
    }
}