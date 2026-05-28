# Models to implement

- [x] document
- [x] components-object
- [x] criterion-expression-type-object
- [x] criterion-object
- [x] failure-action-object
- [x] info
- [x] parameter-object
- [x] payload-replacement-object
- [x] request-body-object
- [x] reusable-object
- [ ] schema
- [x] source-description-object
- [x] specification-extensions
- [x] step-object
- [x] success-action-object
- [x] workflow-object

# features to implement

- [ ] references resolution
- [x] extensions on objects

# open questions

- How is $ref supposed to work for JSON schema fields? externals supported? where can the component schema live in the document?
---> JSON schema (inputs) use $ref
---> everything else the reusable object
