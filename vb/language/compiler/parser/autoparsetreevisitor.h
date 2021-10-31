#pragma once

class AutoParseTreeVisitor : public SimpleParseTreeVisitor<void>
{
    friend class ParseTreeSearcher;
public:
protected:

    virtual void Default(void * );

    virtual void VisitPropertyStatementBase(ParseTree::PropertyStatement * pStatement);
    virtual void VisitStatementBase(ParseTree::Statement * pStatement);
    virtual void VisitParseTreeNodeBase(ParseTree::ParseTreeNode * pParseTreeNode);
    virtual void VisitExpressionBase(ParseTree::Expression * pExpr); 
    virtual void VisitNameBase(ParseTree::Name * pName);
    virtual void VisitTypeBase(ParseTree::Type * pType);
    virtual void VisitConstraintBase(ParseTree::Constraint * pConstraint);
    virtual void VisitVariableDeclarationBase(ParseTree::VariableDeclaration * pVarDecl);
    virtual void VisitCaseBase(ParseTree::Case * pCase);
    virtual void VisitExpressionStatementBase(ParseTree::ExpressionStatement * pExpressionStatement);
    virtual void VisitBlockStatementBase(ParseTree::BlockStatement * pBlockStatement);
    virtual void VisitTypeStatementBase(ParseTree::TypeStatement * pTypeStatement);   
    virtual void VisitMethodSignatureStatementBase(ParseTree::MethodSignatureStatement * pMethodSignatureStatement);
    virtual void VisitMethodDeclarationStatementBase(ParseTree::MethodDeclarationStatement * pStatement);
    virtual void VisitMethodDefinitionStatementBase(ParseTree::MethodDefinitionStatement * pStatement);
    virtual void VisitTypeValueExpressionBase(ParseTree::TypeValueExpression * pTypeValueExpression);    
    virtual void VisitBinaryExpressionBase(ParseTree::BinaryExpression * pBinaryExpression);
    virtual void VisitArrayInitializerExpressionBase(ParseTree::ArrayInitializerExpression * pExpr);
    virtual void VisitSimpleNameBase(ParseTree::SimpleName *pSimpleName);
    virtual void VisitQualifiedNameBase(ParseTree::QualifiedName *pName);
    virtual void VisitHandlerStatementBase(ParseTree::HandlerStatement * pStatement);
    virtual void VisitUnaryExpressionBase(ParseTree::UnaryExpression * pExpr);
    virtual void VisitQualifiedExpressionBase(ParseTree::QualifiedExpression * pExpr);
    virtual void VisitXmlExpressionBase(ParseTree::XmlExpression * pExpr);
    virtual void VisitFromExpressionBase(ParseTree::FromExpression * pExpr);
    virtual void VisitLinqOperatorExpressionBase(ParseTree::LinqOperatorExpression * pExpr);
    virtual void VisitFilterExpressionBase(ParseTree::FilterExpression * pExpr);
    virtual void VisitInnerJoinExpressionBase(ParseTree::InnerJoinExpression * pExpr);
    virtual void VisitSkipTakeExpressionBase(ParseTree::SkipTakeExpression * pExpr);
    virtual void VisitObjectInitializerExpressionBase(ParseTree::ObjectInitializerExpression * pExpr);
    virtual void VisitArrayTypeBase(ParseTree::ArrayType * pType);
    virtual void VisitImportDirectiveBase(ParseTree::ImportDirective * pImportDirective);
    virtual void VisitNamespaceImportDirectiveBase(ParseTree::NamespaceImportDirective * pDirective);
    virtual void VisitInitializerBase(ParseTree::Initializer * pInitializer);
    virtual void VisitForeignMethodDeclarationStatementBase(ParseTree::ForeignMethodDeclarationStatement * pStatement);
    virtual void VisitEnumeratorStatementBase(ParseTree::EnumeratorStatement * pStatement);
    virtual void VisitTypeListStatementBase(ParseTree::TypeListStatement * pStatement);
    virtual void VisitExecutableBlockStatementBase(ParseTree::ExecutableBlockStatement * pStatement);
    virtual void VisitExpressionBlockStatementBase(ParseTree::ExpressionBlockStatement * pStatement);
    virtual void VisitForStatementBase(ParseTree::ForStatement * pStatement);
    virtual void VisitAssignStatementBase(ParseTree::AssignmentStatement * pStatement);
    
    virtual void VisitSyntaxErrorStatement(ParseTree::Statement * pStatement);
    virtual void VisitEmptyStatement(ParseTree::Statement * pStatement);
    virtual void VisitCCConstStatement(ParseTree::CCConstStatement * pStatement);
    virtual void VisitCCBranchStatement(ParseTree::CCBranchStatement* pStatement);
    virtual void VisitCCIfStatement(ParseTree::CCIfStatement * pStatement);
    virtual void VisitCCElseIfStatement(ParseTree::CCIfStatement * pStatement);
    virtual void VisitCCElseStatement(ParseTree::CCElseStatement * pStatement);
    virtual void VisitCCEndIfStatement(ParseTree::CCEndStatement * pStatement);
    virtual void VisitRegionStatement(ParseTree::RegionStatement * pStatement);
    virtual void VisitStructureStatement(ParseTree::TypeStatement * pStatement);
    virtual void VisitEnumStatement(ParseTree::EnumTypeStatement * pStatement);
    virtual void VisitInterfaceStatement(ParseTree::TypeStatement * pStatement);
    virtual void VisitClassStatement(ParseTree::TypeStatement * pStatement);
    virtual void VisitModuleStatement(ParseTree::TypeStatement * pStatement);
    virtual void VisitNamespaceStatement(ParseTree::NamespaceStatement * pStatement);
    virtual void VisitProcedureDeclarationStatement(ParseTree::MethodDefinitionStatement * pStatement);
    virtual void VisitFunctionDeclarationStatement(ParseTree::MethodDefinitionStatement * pStatement);
    virtual void VisitConstructorDeclarationStatement(ParseTree::MethodDefinitionStatement * pStatement);
    virtual void VisitOperatorDeclarationStatement(ParseTree::OperatorDefinitionStatement * pStatement);
    virtual void VisitDelegateProcedureDeclarationStatement(ParseTree::DelegateDeclarationStatement * pStatement);
    virtual void VisitDelegateFunctionDeclarationStatement(ParseTree::DelegateDeclarationStatement * pStatement);
    virtual void VisitEventDeclarationStatement(ParseTree::EventDeclarationStatement * pStatement);
    virtual void VisitBlockEventDeclarationStatement(ParseTree::BlockEventDeclarationStatement * pStatement);
    virtual void VisitAddHandlerDeclarationStatement(ParseTree::MethodDefinitionStatement * pStatement);
    virtual void VisitRemoveHandlerDeclarationStatement(ParseTree::MethodDefinitionStatement * pStatement);
    virtual void VisitRaiseEventDeclarationStatement(ParseTree::MethodDefinitionStatement * pStatement);
    virtual void VisitForeignProcedureDeclarationStatement(ParseTree::ForeignMethodDeclarationStatement * pStatement);
    virtual void VisitForeignFunctionDeclarationStatement(ParseTree::ForeignMethodDeclarationStatement * pStatement);
    virtual void VisitForeignFunctionNoneStatement(ParseTree::ForeignMethodDeclarationStatement * pStatement);
    virtual void VisitPropertyStatement(ParseTree::PropertyStatement * pStatement);
    virtual void VisitAutoPropertyStatement(ParseTree::AutoPropertyStatement * pStatement);
    virtual void VisitPropertyGetStatement(ParseTree::MethodDefinitionStatement * pStatement);
    virtual void VisitPropertySetStatement(ParseTree::MethodDefinitionStatement * pStatement);
    virtual void VisitEnumeratorStatement(ParseTree::EnumeratorStatement * pStatement);
    virtual void VisitEnumeratorWithValueStatement(ParseTree::EnumeratorWithValueStatement * pStatement);
    virtual void VisitVariableDeclarationStatement(ParseTree::VariableDeclarationStatement * pStatement);
    virtual void VisitImplementsStatement(ParseTree::TypeListStatement * pStatement);
    virtual void VisitInheritsStatement(ParseTree::TypeListStatement * pStatement);
    virtual void VisitImportsStatement(ParseTree::ImportsStatement * pStatement);
    virtual void VisitOptionUnknownStatement(ParseTree::OptionStatement * pStatement);
    virtual void VisitOptionInvalidStatement(ParseTree::OptionStatement * pStatement);
    virtual void VisitOptionCompareNoneStatement(ParseTree::OptionStatement * pStatement);
    virtual void VisitOptionCompareTextStatement(ParseTree::OptionStatement * pStatement);
    virtual void VisitOptionCompareBinaryStatement(ParseTree::OptionStatement * pStatement);
    virtual void VisitOptionExplicitOnStatement(ParseTree::OptionStatement * pStatement);
    virtual void VisitOptionExplicitOffStatement(ParseTree::OptionStatement * pStatement);
    virtual void VisitOptionStrictOnStatement(ParseTree::OptionStatement * pStatement);
    virtual void VisitOptionStrictOffStatement(ParseTree::OptionStatement * pStatement);
    virtual void VisitOptionInferOnStatement(ParseTree::OptionStatement * pStatement);
    virtual void VisitOptionInferOffStatement(ParseTree::OptionStatement * pStatement);
    virtual void VisitAttributeStatement(ParseTree::AttributeStatement * pStatement);
    virtual void VisitFileStatement(ParseTree::FileBlockStatement * pStatement);
    virtual void VisitProcedureBodyStatement(ParseTree::MethodBodyStatement * pStatement);
    virtual void VisitPropertyGetBodyStatement(ParseTree::MethodBodyStatement * pStatement);
    virtual void VisitPropertySetBodyStatement(ParseTree::MethodBodyStatement * pStatement);
    virtual void VisitFunctionBodyStatement(ParseTree::MethodBodyStatement * pStatement);
    virtual void VisitOperatorBodyStatement(ParseTree::MethodBodyStatement * pStatement);
    virtual void VisitAddHandlerBodyStatement(ParseTree::MethodBodyStatement * pStatement);
    virtual void VisitRemoveHandlerBodyStatement(ParseTree::MethodBodyStatement * pStatement);
    virtual void VisitRaiseEventBodyStatement(ParseTree::MethodBodyStatement * pStatement);
    virtual void VisitLambdaBodyStatement(ParseTree::LambdaBodyStatement * pStatement);
    virtual void VisitHiddenBlockStatement(ParseTree::HiddenBlockStatement *pStatement);
    virtual void VisitCommentBlockStatement(ParseTree::CommentBlockStatement * pStatement);
    virtual void VisitBlockIfStatement(ParseTree::IfStatement * pStatement);
    virtual void VisitLineIfStatement(ParseTree::IfStatement * pStatement);
    virtual void VisitElseIfStatement(ParseTree::ElseIfStatement * pStatement);
    virtual void VisitBlockElseStatement(ParseTree::ElseStatement * pStatement);
    virtual void VisitLineElseStatement(ParseTree::ElseStatement * pStatement);
    virtual void VisitSelectStatement(ParseTree::SelectStatement * pStatement);
    virtual void VisitCaseStatement(ParseTree::CaseStatement * pStatement);
    virtual void VisitCaseElseStatement(ParseTree::ExecutableBlockStatement * pStatement);
    virtual void VisitTryStatement(ParseTree::ExecutableBlockStatement * pStatement);
    virtual void VisitCatchStatement(ParseTree::CatchStatement * pStatement);
    virtual void VisitFinallyStatement(ParseTree::FinallyStatement * pStatement);
    virtual void VisitForFromToStatement(ParseTree::ForFromToStatement * pStatement);
    virtual void VisitForEachInStatement(ParseTree::ForEachInStatement * pStatement);
    virtual void VisitWhileStatement(ParseTree::ExpressionBlockStatement * pStatement);
    virtual void VisitDoWhileTopTestStatement(ParseTree::TopTestDoStatement * pStatement);
    virtual void VisitDoUntilTopTestStatement(ParseTree::TopTestDoStatement * pStatement);
    virtual void VisitDoWhileBottomTestStatement(ParseTree::ExpressionBlockStatement * pStatement);
    virtual void VisitDoUntilBottomTestStatement(ParseTree::ExpressionBlockStatement * pStatement);
    virtual void VisitDoForeverStatement(ParseTree::ExpressionBlockStatement * pStatement);
    virtual void VisitUsingStatement(ParseTree::UsingStatement * pUsingStatement);
    virtual void VisitWithStatement(ParseTree::ExpressionBlockStatement * pStatement);
    virtual void VisitEndIfStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndUsingStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndWithStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndSelectStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndStructureStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndEnumStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndInterfaceStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndClassStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndModuleStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndNamespaceStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndSubStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndFunctionStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndGetStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndSetStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndPropertyStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndOperatorStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndEventStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndAddHandlerStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndRemoveHandlerStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndRaiseEventStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndNextStatement(ParseTree::EndNextStatement * pStatement);
    virtual void VisitEndWhileStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndLoopWhileStatement(ParseTree::BottomTestLoopStatement * pStatement);
    virtual void VisitEndLoopUntilStatement(ParseTree::BottomTestLoopStatement * pStatement);
    virtual void VisitEndLoopStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndTryStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndSyncLockStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndRegionStatement(ParseTree::CCEndStatement *pStatement);
    virtual void VisitEndCommentBlockStatement(ParseTree::Statement *pStatement);
    virtual void VisitEndUnknownStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitEndInvalidStatement(ParseTree::EndBlockStatement *pStatement);
    virtual void VisitLabelStatement(ParseTree::LabelReferenceStatement *pStatement);
    virtual void VisitGotoStatement(ParseTree::LabelReferenceStatement *pStatement);
    virtual void VisitReturnStatement(ParseTree::ExpressionStatement * pStatement);
    virtual void VisitOnErrorStatement(ParseTree::OnErrorStatement *pStatement);
    virtual void VisitResumeStatement(ParseTree::ResumeStatement *pStatement);
    virtual void VisitCallStatement(ParseTree::CallStatement * pStatement);
    virtual void VisitRaiseEventStatement(ParseTree::RaiseEventStatement * pStatement);
    virtual void VisitAssignStatement(ParseTree::AssignmentStatement * pStatement);
    virtual void VisitAssignPlusStatement(ParseTree::AssignmentStatement * pStatement);
    virtual void VisitAssignMinusStatement(ParseTree::AssignmentStatement * pStatement);
    virtual void VisitAssignMultiplyStatement(ParseTree::AssignmentStatement * pStatement);
    virtual void VisitAssignDivideStatement(ParseTree::AssignmentStatement * pStatement);
    virtual void VisitAssignPowerStatement(ParseTree::AssignmentStatement * pStatement);
    virtual void VisitAssignIntegralDivideStatement(ParseTree::AssignmentStatement * pStatement);
    virtual void VisitAssignConcatenateStatement(ParseTree::AssignmentStatement * pStatement);
    virtual void VisitAssignShiftLeftStatement(ParseTree::AssignmentStatement * pStatement);
    virtual void VisitAssignShiftRightStatement(ParseTree::AssignmentStatement * pStatement);
    virtual void VisitStopStatement(ParseTree::Statement *pStatement);
    virtual void VisitEndStatement(ParseTree::Statement *pStatement);
    virtual void VisitContinueDoStatement(ParseTree::Statement *pStatement);
    virtual void VisitContinueForStatement(ParseTree::Statement *pStatement);
    virtual void VisitContinueWhileStatement(ParseTree::Statement *pStatement);
    virtual void VisitContinueUnknownStatement(ParseTree::Statement *pStatement);
    virtual void VisitContinueInvalidStatement(ParseTree::Statement *pStatement);
    virtual void VisitExitDoStatement(ParseTree::Statement *pStatement);
    virtual void VisitExitForStatement(ParseTree::Statement *pStatement);
    virtual void VisitExitSubStatement(ParseTree::Statement *pStatement);
    virtual void VisitExitFunctionStatement(ParseTree::Statement *pStatement);
    virtual void VisitExitOperatorStatement(ParseTree::Statement *pStatement);
    virtual void VisitExitPropertyStatement(ParseTree::Statement *pStatement);
    virtual void VisitExitTryStatement(ParseTree::Statement *pStatement);
    virtual void VisitExitSelectStatement(ParseTree::Statement *pStatement);
    virtual void VisitExitWhileStatement(ParseTree::Statement *pStatement);
    virtual void VisitExitUnknownStatement(ParseTree::Statement *pStatement);
    virtual void VisitExitInvalidStatement(ParseTree::Statement *pStatement);
    virtual void VisitAssignMidStatement(ParseTree::AssignMidStatement * pStatement);
    virtual void VisitEraseStatement(ParseTree::EraseStatement * pStatement);
    virtual void VisitErrorStatement(ParseTree::ExpressionStatement * pStatement);
    virtual void VisitThrowStatement(ParseTree::ExpressionStatement * pStatement);
    virtual void VisitRedimStatement(ParseTree::RedimStatement * pStatement);
    virtual void VisitAddHandlerStatement(ParseTree::HandlerStatement * pStatement);
    virtual void VisitRemoveHandlerStatement(ParseTree::HandlerStatement * pStatement);
    virtual void VisitSyncLockStatement(ParseTree::ExpressionBlockStatement * pStatement);
    virtual void VisitYieldStatement(ParseTree::ExpressionStatement * pStatement);
    virtual void VisitAwaitStatement(ParseTree::ExpressionStatement * pStatement);
    
    virtual void VisitSyntaxErrorExpression(ParseTree::Expression *pExpr);
    virtual void VisitNameExpression(ParseTree::NameExpression *pExpr);
    virtual void VisitMeExpression(ParseTree::Expression *pExpr);
    virtual void VisitMyBaseExpression(ParseTree::Expression *pExpr);
    virtual void VisitMyClassExpression(ParseTree::Expression *pExpr);
    virtual void VisitGlobalNameSpaceExpression(ParseTree::Expression *pExpr);
    virtual void VisitParenthesizedExpression(ParseTree::ParenthesizedExpression * pExpr);
    virtual void VisitCallOrIndexExpression(ParseTree::CallOrIndexExpression * pExpr);
    virtual void VisitDotQualifiedExpression(ParseTree::QualifiedExpression * pExpr);
    virtual void VisitBangQualifiedExpression(ParseTree::QualifiedExpression * pExpr);
    virtual void VisitXmlElementsQualifiedExpression(ParseTree::QualifiedExpression * pExpr);
    virtual void VisitXmlAttributeQualifiedExpression(ParseTree::QualifiedExpression * pExpr);
    virtual void VisitXmlDescendantsQualifiedExpression(ParseTree::QualifiedExpression * pExpr);
    virtual void VisitGenericQualifiedExpression(ParseTree::GenericQualifiedExpression * pExpr);
    virtual void VisitIntegralLiteralExpression(ParseTree::IntegralLiteralExpression *pExpr);
    virtual void VisitCharacterLiteralExpression(ParseTree::CharacterLiteralExpression *pExpr);
    virtual void VisitBooleanLiteralExpression(ParseTree::BooleanLiteralExpression *pExpr);
    virtual void VisitDecimalLiteralExpression(ParseTree::DecimalLiteralExpression *pExpr);
    virtual void VisitFloatingLiteralExpression(ParseTree::FloatingLiteralExpression *pExpr);
    virtual void VisitDateLiteralExpression(ParseTree::DateLiteralExpression *pExpr);
    virtual void VisitStringLiteralExpression(ParseTree::StringLiteralExpression *pExpr);
    virtual void VisitNothingExpression(ParseTree::Expression *pExpr);
    virtual void VisitCastBooleanExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitCastCharacterExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitCastDateExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitCastDoubleExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitCastSignedByteExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitCastByteExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitCastShortExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitCastUnsignedShortExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitCastIntegerExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitCastUnsignedIntegerExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitCastLongExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitCastUnsignedLongExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitCastDecimalExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitCastSingleExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitCastStringExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitCastObjectExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitConversionExpression(ParseTree::ConversionExpression * pExpr);
    virtual void VisitDirectCastExpression(ParseTree::ConversionExpression * pExpr);
    virtual void VisitTryCastExpression(ParseTree::ConversionExpression * pExpr);
    virtual void VisitNegateExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitNotExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitUnaryPlusExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitAddressOfExpression(ParseTree::UnaryExpression * pExpr);
    virtual void VisitPlusExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitMinusExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitMultiplyExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitDivideExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitPowerExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitIntegralDivideExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitConcatenateExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitShiftLeftExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitShiftRightExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitModulusExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitOrExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitOrElseExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitXorExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitAndExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitAndAlsoExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitLikeExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitIsExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitIsNotExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitEqualExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitNotEqualExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitLessExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitLessEqualExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitGreaterEqualExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitGreaterExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitXmlDocumentExpression(ParseTree::XmlDocumentExpression * pExpr);
    virtual void VisitXmlElementExpression(ParseTree::XmlElementExpression * pExpr);
    virtual void VisitXmlAttributeExpression(ParseTree::XmlAttributeExpression * pExpr);
    virtual void VisitXmlAttributeValueListExpression(ParseTree::XmlExpression * pExpr);
    virtual void VisitXmlNameExpression(ParseTree::XmlNameExpression *pExpr);
    virtual void VisitXmlCharDataExpression(ParseTree::XmlCharDataExpression *pExpr);
    virtual void VisitXmlCDataExpression(ParseTree::XmlExpression * pExpr);
    virtual void VisitXmlPIExpression(ParseTree::XmlPIExpression * pExpr);
    virtual void VisitXmlCommentExpression(ParseTree::XmlExpression * pExpr);
    virtual void VisitXmlReferenceExpression(ParseTree::XmlReferenceExpression *pExpr);
    virtual void VisitXmlEmbeddedExpression(ParseTree::XmlEmbeddedExpression * pExpr);
    virtual void VisitFromExpression(ParseTree::FromExpression * pExpr);
    virtual void VisitLetExpression(ParseTree::FromExpression * pExpr);
    virtual void VisitAggregateExpression(ParseTree::AggregateExpression * pExpr);
    virtual void VisitQueryAggregateGroupExpression(ParseTree::QueryAggregateGroupExpression * pExpr);
    virtual void VisitCrossJoinExpression(ParseTree::CrossJoinExpression * pExpr);
    virtual void VisitWhereExpression(ParseTree::WhereExpression * pExpr);
    virtual void VisitSelectExpression(ParseTree::SelectExpression * pExpr);
    virtual void VisitGroupByExpression(ParseTree::GroupByExpression * pExpr);
    virtual void VisitGroupRefExpression(ParseTree::Expression *pExpr);
    virtual void VisitAggregationExpression(ParseTree::AggregationExpression * pExpr);
    virtual void VisitQueryOperatorCallExpression(ParseTree::QueryOperatorCallExpression * pExpr);
    virtual void VisitDistinctExpression(ParseTree::DistinctExpression * pExpr);
    virtual void VisitOrderByExpression(ParseTree::OrderByExpression * pExpr);
    virtual void VisitLinqSourceExpression(ParseTree::LinqSourceExpression * pExpr);
    virtual void VisitInnerJoinExpression(ParseTree::InnerJoinExpression * pExpr);
    virtual void VisitGroupJoinExpression(ParseTree::GroupJoinExpression * pExpr);
    virtual void VisitEqualsExpression(ParseTree::BinaryExpression * pExpr);
    virtual void VisitTakeWhileExpression(ParseTree::WhileExpression * pExpr);
    virtual void VisitSkipWhileExpression(ParseTree::WhileExpression * pExpr);
    virtual void VisitTakeExpression(ParseTree::SkipTakeExpression * pExpr);
    virtual void VisitSkipExpression(ParseTree::SkipTakeExpression * pExpr);
    virtual void VisitImplicitConversionExpression(ParseTree::ImplicitConversionExpression * pExpr);
    virtual void VisitIsTypeExpression(ParseTree::TypeValueExpression * pExpr);
    virtual void VisitTypeReferenceExpression(ParseTree::TypeReferenceExpression * pExpr);
    virtual void VisitNewExpression(ParseTree::NewExpression * pExpr);
    virtual void VisitArrayInitializerExpression(ParseTree::ArrayInitializerExpression * pExpr);
    virtual void VisitNewArrayInitializerExpression(ParseTree::NewArrayInitializerExpression * pExpr);
    virtual void VisitNewObjectInitializerExpression(ParseTree::NewObjectInitializerExpression* pExpr);
    virtual void VisitGetTypeExpression(ParseTree::GetTypeExpression * pExpr);
    virtual void VisitGetXmlNamespaceExpression(ParseTree::GetXmlNamespaceExpression *pExpr);
    virtual void VisitLambdaExpression(ParseTree::LambdaExpression * pExpr);
    virtual void VisitIIfExpression(ParseTree::IIfExpression * pExpr);
    virtual void VisitCollectionInitializerExpression(ParseTree::CollectionInitializerExpression * pExpr);
    virtual void VisitAlreadyBoundExpression(ParseTree::AlreadyBoundExpression * pExpr);
    virtual void VisitAlreadyBoundSymbolExpression(ParseTree::AlreadyBoundSymbolExpression *pExpr);
    virtual void VisitDeferredExpression(ParseTree::DeferredExpression * pExpr);
    virtual void VisitAwaitExpression(ParseTree::UnaryExpression * pExpr);

    virtual void VisitSimpleName(ParseTree::SimpleName * pName);
    virtual void VisitSimpleWithArgumentsName(ParseTree::SimpleWithArgumentsName * pName);
    virtual void VisitQualifiedName(ParseTree::QualifiedName * pName);
    virtual void VisitQualifiedWithArgumentsName(ParseTree::QualifiedWithArgumentsName * pName);
    virtual void VisitGlobalNameSpaceName(ParseTree::Name *pName);

    virtual void VisitSyntaxErrorType(ParseTree::Type *pType);
    virtual void VisitBooleanType(ParseTree::Type *pType);
    virtual void VisitSignedByteType(ParseTree::Type *pType);
    virtual void VisitByteType(ParseTree::Type *pType);
    virtual void VisitShortType(ParseTree::Type *pType);
    virtual void VisitUnsignedShortType(ParseTree::Type *pType);
    virtual void VisitIntegerType(ParseTree::Type *pType);
    virtual void VisitUnsignedIntegerType(ParseTree::Type *pType);
    virtual void VisitLongType(ParseTree::Type *pType);
    virtual void VisitUnsignedLongType(ParseTree::Type *pType);
    virtual void VisitDecimalType(ParseTree::Type *pType);
    virtual void VisitSingleType(ParseTree::Type *pType);
    virtual void VisitDoubleType(ParseTree::Type *pType);
    virtual void VisitDateType(ParseTree::Type *pType);
    virtual void VisitCharType(ParseTree::Type *pType);
    virtual void VisitStringType(ParseTree::Type *pType);
    virtual void VisitObjectType(ParseTree::Type *pType);
    virtual void VisitAlreadyBoundType(ParseTree::AlreadyBoundType *pType);
    virtual void VisitAlreadyBoundDelayCalculatedType(ParseTree::AlreadyBoundDelayCalculatedType *pType);
    virtual void VisitNamedType(ParseTree::NamedType * pType);
    virtual void VisitArrayWithoutSizesType(ParseTree::ArrayType * pType);
    virtual void VisitArrayWithSizesType(ParseTree::ArrayWithSizesType * pType);
    virtual void VisitNullableType(ParseTree::NullableType * pType);

    virtual void VisitNoInitializerVariableDeclaration(ParseTree::VariableDeclaration * pVarDecl);
    virtual void VisitWithInitializerVariableDeclaration(ParseTree::InitializerVariableDeclaration * pVarDecl);
    virtual void VisitWithNewVariableDeclaration(ParseTree::NewVariableDeclaration * pVarDecl);

    virtual void VisitNewConstraint(ParseTree::Constraint *pConstraint);
    virtual void VisitClassConstraint(ParseTree::Constraint *pConstraint);
    virtual void VisitStructConstraint(ParseTree::Constraint *pConstraint);
    virtual void VisitTypeConstraint(ParseTree::TypeConstraint * pConstraint);

    virtual void VisitNamespaceImportDirective(ParseTree::NamespaceImportDirective * pDirective);
    virtual void VisitAliasImportDirective(ParseTree::AliasImportDirective * pDirective);
    virtual void VisitXmlNamespaceImportDirective(ParseTree::XmlNamespaceImportDirective * pDirective);

    virtual void VisitSyntaxErrorCase(ParseTree::Case * pCase);
    virtual void VisitRelationalCase(ParseTree::RelationalCase * pCase);  
    virtual void VisitValueCase(ParseTree::ValueCase * pCase);
    virtual void VisitRangeCase(ParseTree::RangeCase * pCase);

    virtual void VisitExpressionInitializer(ParseTree::ExpressionInitializer * pInitializer);
    virtual void VisitDeferredInitializer(ParseTree::DeferredInitializer * pInitializer);
    virtual void VisitAssignmentInitializer(ParseTree::AssignmentInitializer * pInitializer);

    virtual void VisitAutoPropertyInitBase(ParseTree::AutoPropertyInitialization * pInit);
    virtual void VisitWithInitializerAutoPropInit(ParseTree::InitializerAutoPropertyDeclaration * pInit);
    virtual void VisitWithNewAutoPropInit (ParseTree::NewAutoPropertyDeclaration * pInit);
    

    virtual void VisitAttributeList(ParseTree::AttributeList * pAttributeList);
    virtual void VisitSpecifierList(ParseTree::SpecifierList * pSpecifierList);
    virtual void VisitAttributeSpecifierList(ParseTree::AttributeSpecifierList * pAttributeSpecifierList);
    virtual void VisitConstraintList(ParseTree::ConstraintList * pConstraintList);
    virtual void VisitGenericParameterList(ParseTree::GenericParameterList * pGenericParameterList);
    virtual void VisitParameterSpecifierList(ParseTree::ParameterSpecifierList * pParameterSpecifierList);
    virtual void VisitParameterList(ParseTree::ParameterList * pParameterList);
    virtual void VisitDeclaratorList(ParseTree::DeclaratorList * pDeclaratorList);
    virtual void VisitVariableDeclarationList(ParseTree::VariableDeclarationList * pVariableDeclarationList);
    virtual void VisitImportDirectiveList(ParseTree::ImportDirectiveList * pImportDirectiveList);
    virtual void VisitCaseList(ParseTree::CaseList * pCaseList);
    virtual void VisitCommentList(ParseTree::CommentList * pCommentList);
    virtual void VisitNameList(ParseTree::NameList * pNameList);
    virtual void VisitTypeList(ParseTree::TypeList * pTypeList);
    virtual void VisitArgumentList(ParseTree::ArgumentList * pArgumentList);    
    virtual void VisitExpressionList(ParseTree::ExpressionList * pExpressionList);
    virtual void VisitArrayDimList(ParseTree::ArrayDimList * pArrayDimList);
    virtual void VisitFromList(ParseTree::FromList * pFromList);
    virtual void VisitOrderByList(ParseTree::OrderByList * pOrderByList);
    virtual void VisitInitializerList(ParseTree::InitializerList * pInitializerList);
    virtual void VisitParenthesizedArgumentList(ParseTree::ParenthesizedArgumentList * pArgumentList);
    virtual void VisitAttribute(ParseTree::Attribute * pAttribute);
    virtual void VisitArgument(ParseTree::Argument * pArgument);
    virtual void VisitBracedInitializerList(ParseTree::BracedInitializerList * pInitializerList);
    virtual void VisitExternalSourceDirective(ParseTree::ExternalSourceDirective * pExternalSourceDirective);
    virtual void VisitStatementList(ParseTree::StatementList * pStatementList);  
    virtual void VisitGenericArguments(ParseTree::GenericArguments * pGenericArguments);
    virtual void VisitAttributeSpecifier(ParseTree::AttributeSpecifier * pAttributeSpecifier);
    virtual void VisitGenericParameter(ParseTree::GenericParameter * pParam);
    virtual void VisitParameter(ParseTree::Parameter * pParam);
    virtual void VisitDeclarator(ParseTree::Declarator * pDeclarator);
    virtual void VisitComment(ParseTree::Comment * pComment);
    virtual void VisitArrayDim(ParseTree::ArrayDim * pArrayDim);
    virtual void VisitFromItem(ParseTree::FromItem * pFromItem);
    virtual void VisitOrderByItem(ParseTree::OrderByItem * pOrderByItem);
    virtual void VisitObjectInitializerList(ParseTree::ObjectInitializerList * pInitList);
    virtual void VisitSpecifier(ParseTree::Specifier * pSpecifier);
    virtual void VisitParameterSpecifier(ParseTree::ParameterSpecifier * pSpecifier);

private:

    template <class ElementType, class ListType, class visit_function>
    void VisitList(ParseTree::List<ElementType, ListType> * pList, visit_function visit_func)
    {
        while (pList)
        {
            VisitParseTreeNodeBase(pList);
            if (pList->Element)
            {
                (this->*visit_func)(pList->Element);
            }
            pList = pList->Next;
        }
    }
    
};
