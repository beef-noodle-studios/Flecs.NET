namespace NoodleStudios.Flecs.Pairs;

public static class Pair
{
    public readonly struct WithRelationId(Id relation)
    {
        private readonly Id _relation = relation;

        public TagResolver Target(Id target)
        {
            return new TagResolver(_relation, target);
        }

        public ComponentResolverWithTargetType<TTarget> Target<TTarget>()
            where TTarget : unmanaged
        {
            return new ComponentResolverWithTargetType<TTarget>(_relation);
        }

        public ComponentWithTargetValue<TTarget> Target<TTarget>(TTarget target)
            where TTarget : unmanaged
        {
            return new ComponentWithTargetValue<TTarget>(_relation, target);
        }
    }

    public readonly struct WithRelationType<TRelation>
        where TRelation : unmanaged
    {
        public ComponentWithTargetValue<TRelation, TTarget> Target<TTarget>(TTarget targetValue)
            where TTarget : unmanaged
        {
            return new ComponentWithTargetValue<TRelation, TTarget>(targetValue);
        }

        public ComponentResolverWithRelationType<TRelation> Target(Id target)
        {
            return new ComponentResolverWithRelationType<TRelation>(target);
        }

        public ComponentResolver<TRelation, TTarget> Target<TTarget>()
            where TTarget : unmanaged
        {
            return new ComponentResolver<TRelation, TTarget>();
        }
    }

    public readonly struct WithRelationValue<TRelation>(TRelation relation)
        where TRelation : unmanaged
    {
        public readonly TRelation Relation = relation;

        public ComponentWithRelationValue<TRelation> Target(Id target)
        {
            return new ComponentWithRelationValue<TRelation>(Relation, target);
        }

        public ComponentWithRelationValue<TRelation, TTarget> Target<TTarget>()
            where TTarget : unmanaged
        {
            return new ComponentWithRelationValue<TRelation, TTarget>(Relation);
        }
    }

    public readonly struct TagResolver(Id relation, Id target)
    {
        public readonly Id Relation = relation;
        public readonly Id Target = target;
    }

    public readonly struct TagResolver<TRelation, TTarget>()
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
    }

    public readonly struct ComponentResolverWithRelationType<TRelation>(Id target)
        where TRelation : unmanaged
    {
        public readonly Id Target = target;
    }

    public readonly struct ComponentResolverWithTargetType<TTarget>(Id relation)
        where TTarget : unmanaged
    {
        public readonly Id Relation = relation;
    }

    public readonly struct ComponentResolver<TRelation, TTarget>
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
    }

    public readonly struct ComponentWithRelationValue<TRelation>(TRelation relation, Id target)
        where TRelation : unmanaged
    {
        public readonly TRelation RelationValue = relation;
        public readonly Id Target = target;
    }

    public readonly struct ComponentWithRelationValue<TRelation, TTarget>(TRelation relation)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        public readonly TRelation Relation = relation;
    }

    public readonly struct ComponentWithTargetValue<TTarget>(Id relation, TTarget target)
        where TTarget : unmanaged
    {
        public readonly Id Relation = relation;
        public readonly TTarget Target = target;
    }

    public readonly struct ComponentWithTargetValue<TRelation, TTarget>(TTarget TargetValue)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        public readonly TTarget TargetValue = TargetValue;
    }

    public static WithRelationId Relation(Id relation)
    {
        return new WithRelationId(relation);
    }

    public static WithRelationType<TRelation> Relation<TRelation>()
        where TRelation : unmanaged
    {
        return new WithRelationType<TRelation>();
    }

    public static WithRelationValue<TRelation> Relation<TRelation>(TRelation relation)
        where TRelation : unmanaged
    {
        return new WithRelationValue<TRelation>(relation);
    }

    public static TagResolver Tag(Id relation, Id target)
    {
        return new TagResolver(relation, target);
    }

    public static TagResolver<TRelation, TTarget> Tag<TRelation, TTarget>()
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new TagResolver<TRelation, TTarget>();
    }

    public static ComponentWithRelationValue<TRelation, TTarget> Component<TRelation, TTarget>(TRelation relation)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new ComponentWithRelationValue<TRelation, TTarget>(relation);
    }

    public static ComponentWithTargetValue<TRelation, TTarget> Component<TRelation, TTarget>(TTarget target)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new ComponentWithTargetValue<TRelation, TTarget>(target);
    }

    public static ComponentResolver<TRelation, TTarget> Component<TRelation, TTarget>()
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new ComponentResolver<TRelation, TTarget>();
    }
}
