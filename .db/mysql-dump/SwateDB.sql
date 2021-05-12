-- Adminer 4.7.7 MySQL dump

SET NAMES utf8;
SET time_zone = '+00:00';
SET foreign_key_checks = 0;
SET sql_mode = 'NO_AUTO_VALUE_ON_ZERO';

DELIMITER ;;

DROP PROCEDURE IF EXISTS `advancedTermSearch`;;
CREATE PROCEDURE `advancedTermSearch`(IN `ontologyName` varchar(256), IN `searchTermName` varchar(512), IN `mustContainName` varchar(512), IN `searchTermDefinition` varchar(512), IN `mustContainDefinition` varchar(512))
BEGIN
    IF searchTermName = '' THEN SET searchTermName = NULL; END IF;
    IF mustContainName = '' THEN SET mustContainName = NULL; END IF;
    IF searchTermDefinition = '' THEN SET searchTermDefinition = NULL; END IF;
    IF mustContainDefinition = '' THEN SET mustContainDefinition = NULL; END IF;
	IF ISNULL(ontologyName) THEN
		Call advancedTermSearchByNameByDefinition(searchTermName,mustContainName,searchTermDefinition,mustContainDefinition);
	ELSE
		Call advancedTermSearchByOntByNameByDefinition(ontologyName,searchTermName,mustContainName,searchTermDefinition,mustContainDefinition);
    END IF;
END;;

DROP PROCEDURE IF EXISTS `advancedTermSearchByNameByDefinition`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `advancedTermSearchByNameByDefinition`(
	IN searchTermName varchar(512),
    IN mustContainName varchar(512),
    IN searchTermDefinition varchar(512),
    IN mustContainDefinition varchar(512)
)
BEGIN
	SELECT * 
		FROM Term
		WHERE
			(ISNULL(searchTermName) 
				OR MATCH(Term.Name) AGAINST(searchTermName IN BOOLEAN MODE)
			)
			AND (ISNULL(mustContainName) 
				OR INSTR(Term.Name,mustContainName) > 0
			)
            AND (ISNULL(searchTermDefinition) 
				OR Match(Term.Definition) AGAINST(searchTermDefinition IN BOOLEAN MODE)
			)
            AND (ISNULL(mustContainDefinition) 
				OR INSTR(Term.Definition,mustContainDefinition) > 0
			);
END;;

DROP PROCEDURE IF EXISTS `advancedTermSearchByOntByNameByDefinition`;;
CREATE PROCEDURE `advancedTermSearchByOntByNameByDefinition`(IN `ontologyName` varchar(256), IN `searchTermName` varchar(512), IN `mustContainName` varchar(512), IN `searchTermDefinition` varchar(512), IN `mustContainDefinition` varchar(512))
BEGIN
	SELECT * 
		FROM Term
		WHERE
			(ISNULL(searchTermName) 
				OR MATCH(Term.Name) AGAINST(searchTermName IN BOOLEAN MODE)
			)
			AND (ISNULL(mustContainName) 
				OR INSTR(Term.Name,mustContainName) > 0
			)
            AND (ISNULL(searchTermDefinition) 
				OR Match(Term.Definition) AGAINST(searchTermDefinition IN BOOLEAN MODE)
			)
            AND (ISNULL(mustContainDefinition) 
				OR INSTR(Term.Definition,mustContainDefinition) > 0
			)
            AND Term.FK_OntologyName = ontologyName;
END;;

DROP PROCEDURE IF EXISTS `getAllOntologies`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `getAllOntologies`()
BEGIN
	SELECT * FROM Ontology;
END;;

DROP PROCEDURE IF EXISTS `getAllTermsByChildTerm`;;
CREATE PROCEDURE `getAllTermsByChildTerm`(IN `childOntology` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, fk_ontologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession_Related
			AND trt.FK_TermAccession = ref.Accession
			AND
				( 
					trt.FK_TermAccession = ref.Accession
					AND ref.Name = childOntology
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession_Related
			AND trt2.FK_TermAccession = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
	);
END;;

DROP PROCEDURE IF EXISTS `getAllTermsByChildTermAndAccession`;;
CREATE PROCEDURE `getAllTermsByChildTermAndAccession`(IN `childOntology` varchar(512), IN `childTermAccession` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession_Related
			AND trt.FK_TermAccession = ref.Accession
			AND
				( 
					trt.FK_TermAccession = ref.Accession
					AND ref.Name = childOntology
                    AND ref.Accession = childTermAccession
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession_Related
			AND trt2.FK_TermAccession = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
	);
END;;

DROP PROCEDURE IF EXISTS `getAllTermsByParentTerm`;;
CREATE PROCEDURE `getAllTermsByParentTerm`(IN `parentOntology` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession
			AND trt.FK_TermAccession_Related = ref.Accession
			AND
				( 
					trt.FK_TermAccession_Related = ref.Accession
					AND ref.Name = parentOntology
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession
			AND trt2.FK_TermAccession_Related = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
	);
END;;

DROP PROCEDURE IF EXISTS `getAllTermsByParentTermAndAccession`;;
CREATE PROCEDURE `getAllTermsByParentTermAndAccession`(IN `parentOntology` varchar(512), IN `parentTermAccession` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession
			AND trt.FK_TermAccession_Related = ref.Accession
			AND
				( 
					trt.FK_TermAccession_Related = ref.Accession
					AND ref.Name = parentOntology
                    AND ref.Accession = parentTermAccession
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession
			AND trt2.FK_TermAccession_Related = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
	);
END;;

DROP PROCEDURE IF EXISTS `getMSTermSuggestions`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `getMSTermSuggestions`(
	IN queryParam varchar(512)
)
BEGIN
	CALL getTermSuggestionsByOntology(queryParam,'ms');
END;;

DROP PROCEDURE IF EXISTS `getPlantExpConditionsOntoTermSuggestions`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `getPlantExpConditionsOntoTermSuggestions`(
	IN queryParam varchar(512)
)
BEGIN
	CALL getTermSuggestionsByOntology(queryParam,'peco');
END;;

DROP PROCEDURE IF EXISTS `getPlantOntoTermSuggestions`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `getPlantOntoTermSuggestions`(
	IN queryParam varchar(512)
)
BEGIN
	CALL getTermSuggestionsByOntology(queryParam,'po');
END;;

DROP PROCEDURE IF EXISTS `getPlantTraitOntoTermSuggestions`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `getPlantTraitOntoTermSuggestions`(
	IN queryParam varchar(512)
)
BEGIN
	CALL getTermSuggestionsByOntology(queryParam,'to');
END;;

DROP PROCEDURE IF EXISTS `getTermByParentTerm`;;
CREATE PROCEDURE `getTermByParentTerm`(IN `query` varchar(512), IN `parentOntology` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession
			AND trt.FK_TermAccession_Related = ref.Accession
			AND
				( 
					trt.FK_TermAccession_Related = ref.Accession
					AND ref.Name = parentOntology
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession
			AND trt2.FK_TermAccession_Related = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
		AND
			(
				t.Name = query
			)
	);
END;;

DROP PROCEDURE IF EXISTS `getTermByParentTermAndAccession`;;
CREATE PROCEDURE `getTermByParentTermAndAccession`(IN `query` varchar(512), IN `parentOntology` varchar(512), IN `parentTermAccession` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession
			AND trt.FK_TermAccession_Related = ref.Accession
			AND
				( 
					trt.FK_TermAccession_Related = ref.Accession
					AND ref.Name = parentOntology
                    AND ref.Accession = parentTermAccession
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession
			AND trt2.FK_TermAccession_Related = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
		AND
			(
				t.Name = query
			)
	);
END;;

DROP PROCEDURE IF EXISTS `getTermSuggestions`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `getTermSuggestions`(IN `query` varchar(512))
BEGIN 
SELECT * 
FROM Term 
WHERE 
	(
		MATCH(Term.Name) AGAINST(query) 
	);
END;;

DROP PROCEDURE IF EXISTS `getTermSuggestionsByChildTerm`;;
CREATE PROCEDURE `getTermSuggestionsByChildTerm`(IN `query` varchar(512), IN `childOntology` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, fk_ontologyid, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession_Related
			AND trt.FK_TermAccession = ref.Accession
			AND
				( 
					trt.FK_TermAccession = ref.Accession
					AND ref.Name = childOntology
				)
		)
		UNION All
		SELECT  
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession_Related
			AND trt2.FK_TermAccession = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
        AND
			(
				Match(t.Name) AGAINST(Concat(query,'*') IN BOOLEAN MODE) 
				OR INSTR(t.Name,query) > 0
			)
	);
END;;

DROP PROCEDURE IF EXISTS `getTermSuggestionsByChildTermAndAccession`;;
CREATE PROCEDURE `getTermSuggestionsByChildTermAndAccession`(IN `query` varchar(512), IN `childOntology` varchar(512), IN `childTermAccession` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession_Related
			AND trt.FK_TermAccession = ref.Accession
			AND
				( 
					trt.FK_TermAccession = ref.Accession
					AND ref.Name = childOntology
                    AND ref.Accession = childTermAccession
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession_Related
			AND trt2.FK_TermAccession = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
        AND
			(
				Match(t.Name) AGAINST(Concat(query,'*') IN BOOLEAN MODE) 
				OR INSTR(t.Name,query) > 0
			)
	);
END;;

DROP PROCEDURE IF EXISTS `getTermSuggestionsByOntology`;;
CREATE PROCEDURE `getTermSuggestionsByOntology`(IN `queryParam` varchar(512), IN `ontologyParam` varchar(512))
BEGIN
    SELECT * 
		FROM Term 
        WHERE 
			(
				INSTR(Term.Name,queryParam) > 0
				OR Match(Term.Name) AGAINST(Concat(queryParam,'*') IN BOOLEAN MODE)
			)
			AND FK_OntologyName = ontologyParam;
END;;

DROP PROCEDURE IF EXISTS `getTermSuggestionsByParentTerm`;;
CREATE PROCEDURE `getTermSuggestionsByParentTerm`(IN `query` varchar(512), IN `parentOntology` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession
			AND trt.FK_TermAccession_Related = ref.Accession
			AND
				( 
					trt.FK_TermAccession_Related = ref.Accession
					AND ref.Name = parentOntology
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession
			AND trt2.FK_TermAccession_Related = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
		AND
			(
				Match(t.Name) AGAINST(Concat(query,'*') IN BOOLEAN MODE) 
				OR INSTR(t.Name,query) > 0
			)
	);
END;;

DROP PROCEDURE IF EXISTS `getTermSuggestionsByParentTermAndAccession`;;
CREATE PROCEDURE `getTermSuggestionsByParentTermAndAccession`(IN `query` varchar(512), IN `parentOntology` varchar(512), IN `parentTermAccession` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession
			AND trt.FK_TermAccession_Related = ref.Accession
			AND
				( 
					trt.FK_TermAccession_Related = ref.Accession
					AND ref.Name = parentOntology
                    AND ref.Accession = parentTermAccession
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession
			AND trt2.FK_TermAccession_Related = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
		AND
			(
				Match(t.Name) AGAINST(Concat(query,'*') IN BOOLEAN MODE) 
				OR INSTR(t.Name,query) > 0
			)
	);
END;;

DROP PROCEDURE IF EXISTS `getUnitTermSuggestions`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `getUnitTermSuggestions`(
	IN queryParam varchar(512)
)
BEGIN
	CALL getTermSuggestionsByOntology(queryParam,'uo');
END;;

DELIMITER ;

SET NAMES utf8mb4;

DROP TABLE IF EXISTS `Ontology`;
CREATE TABLE `Ontology` (
  `Name` varchar(256) NOT NULL,
  `CurrentVersion` varchar(256) NOT NULL,
  `Definition` varchar(1024) NOT NULL,
  `DateCreated` datetime(6) NOT NULL,
  `UserID` varchar(32) NOT NULL,
  PRIMARY KEY (`Name`),
  KEY `Ind_Ontology_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `Ontology` (`Name`, `CurrentVersion`, `Definition`, `DateCreated`, `UserID`) VALUES
('chebi',	'194',	'Chemical Entities of Biological Interest',	'2020-11-27 18:55:00.000000',	'Pier Luigi Buttigieg'),
('envo',	'releases/2020-06-10',	'Environment Ontology',	'2020-06-10 00:00:00.000000',	'chebi'),
('go',	'releases/2020-11-18',	'gene_ontology',	'2020-11-18 00:00:00.000000',	'Suzi Aleksander'),
('ms',	'4.1.35',	'Proteomics Standards Initiative Mass Spectrometry Vocabularies',	'2020-02-17 15:39:00.000000',	'Gerhard Mayer'),
('ncbitaxon',	'2020-04-18',	'NCBI organismal classification',	'2020-04-18 00:00:00.000000',	'Frederic Bastian'),
('nfdi4pso',	'init/2020-12-01',	'nfdi4pso',	'2020-12-01 00:00:00.000000',	'muehlhaus'),
('obi',	'obi/2020-08-24/obi.obo',	'Ontology for Biomedical Investigations',	'2020-08-24 14:31:00.000000',	'Bjoern Peters'),
('pato',	'releases/2020-08-02/pato.obo',	'Phenotype And Trait Ontology',	'2020-08-02 00:00:00.000000',	'George Gkoutos'),
('peco',	'releases/2020-08-21',	'Plant Experimental Conditions Ontology',	'2015-10-21 15:21:00.000000',	'cooperl'),
('po',	'releases/2020-08-20',	'Plant Ontology',	'2020-08-20 00:00:00.000000',	'cooperl'),
('ro',	'releases/2020-07-21',	'Relation Ontology',	'2020-07-21 00:00:00.000000',	'Chris Mungall'),
('to',	'releases/2020-10-13',	'Plant Trait Ontology',	'2020-08-20 00:00:00.000000',	'cooperl'),
('uo',	'releases/2020-03-10',	'Unit Ontology',	'2014-09-04 13:37:00.000000',	'gkoutos');

DROP TABLE IF EXISTS `Protocol`;
CREATE TABLE `Protocol` (
  `Name` varchar(512) NOT NULL,
  `Version` varchar(128) NOT NULL,
  `Created` datetime NOT NULL DEFAULT current_timestamp(),
  `Author` varchar(256) NOT NULL,
  `Description` text NOT NULL,
  `DocsLink` varchar(1024) NOT NULL,
  `Tags` varchar(1024) NOT NULL,
  `Used` int(10) unsigned NOT NULL,
  `Rating` int(10) unsigned NOT NULL,
  PRIMARY KEY (`Name`),
  KEY `Version` (`Version`),
  KEY `Ind_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `Protocol` (`Name`, `Version`, `Created`, `Author`, `Description`, `DocsLink`, `Tags`, `Used`, `Rating`) VALUES
('Data Processing for Proteomics',	'1.0.0',	'2021-03-11 17:04:29',	'Oliver Maus',	'This protocol tackles the steps after obtaining measurement results in terms of computational analysis.',	'https://github.com/nfdi4plants/SWATE_templates/wiki/4COM02_ProteomicsDataProcessing',	'4_COM;Computational analyses;Proteomics;Data procession;Software;Analysis;er:ISA;mod:4COM',	5,	0),
('Extraction for Proteomics',	'1.0.0',	'2021-03-12 12:15:01',	'Oliver Maus',	'This protocol focuses on lab works regarding the extraction of the molecule of interest.',	'https://github.com/nfdi4plants/SWATE_templates/wiki/2EXT02_ProteomicsExtraction',	'2_EXT;Extraction;Proteomics;er:ISA;mod:2EXT',	0,	0),
('Measurement for Proteomics',	'1.0.0',	'2021-03-12 12:15:01',	'Oliver Maus',	'This protocol focuses on the measurement of the mass spectrometer, its settings and all other relevant data related to this.',	'https://github.com/nfdi4plants/SWATE_templates/wiki/3ASY02_ProteomicsMeasurement',	'3_ASY;Assay;Proteomics;Measurement;Mass spectrometry;MS;er:ISA;mod:3ASY',	3,	0),
('Metabolite Extraction',	'1.0.0',	'2021-03-22 15:45:38',	'Dominik Brilhaus',	'Template to describe the extraction of metabolites for a metabolomics assay.',	'https://github.com/nfdi4plants/SWATE_templates/wiki/2EXT03_Metabolites',	'Extraction;Metabolites;er:Metabolights;mod:2EXT',	0,	0),
('Metabolomics Assay',	'1.0.0',	'2021-03-22 15:45:38',	'Dominik Brilhaus',	'Template to describe preparation and measurement of metabolomics samples',	'https://github.com/nfdi4plants/SWATE_templates/wiki/3ASY03_Metabolomics',	'Metabolomics;metabolites;Assay;er:Metabolights;mod:3ASY',	0,	0),
('Plant growth',	'1.0.1',	'2021-03-12 13:35:10',	'Hajira Jabeen,Dominik Brilhaus',	'Template to describe a plant growth study as well as sample collection and handling.',	'https://github.com/nfdi4plants/SWATE_templates/wiki/1SPL01_plants',	'Plants;Sampling;Plant growth;Plant study;er:GEO;er:MetaboLights;mod:1SPL',	17,	0),
('RNA extraction',	'1.0.2',	'2021-03-12 14:49:38',	'Hajira Jabeen,Dominik Brilhaus',	'Template to describe the extraction of RNA.',	'https://github.com/nfdi4plants/SWATE_templates/wiki/2EXT01_RNA',	'Extraction;RNA;er:GEO;mod:2EXT',	2,	0),
('RNA-Seq Assay',	'1.0.0',	'2021-03-11 17:04:29',	'Hajira Jabeen,Dominik Brilhaus',	'Template to describe preparation and measurement of RNA-Seq.',	'https://github.com/nfdi4plants/SWATE_templates/wiki/3ASY01_RNASeqGEO',	'Transcriptomics;mRNASeq;RNASeq;Assay;er:GEO;mod:3ASY',	3,	0),
('RNA-Seq Computational Analysis',	'1.0.0',	'2021-03-11 17:04:29',	'Hajira Jabeen,Dominik Brilhaus',	'Template to describe the computational analysis of RNA-Seq data.',	'https://github.com/nfdi4plants/SWATE_templates/blob/main/templates/4COM01_RNASeqGEO.json',	'Transcriptomics;mRNASeq;RNASeq;Computational Analysis;er:GEO;mod:4COM',	2,	0),
('Sample Preparation for Proteomics',	'1.0.0',	'2021-03-12 12:15:01',	'Oliver Maus',	'This protocol focuses on works in the lab the experimentator does, e.g. cultivation of cells, transfection with genes, and so on. It is specialized to work for preparations for proteomics experiments. It includes special annotation blocks for proteomics workflows.',	'https://github.com/nfdi4plants/SWATE_templates/wiki/1SPL02_ProteomicsSamplePreparation',	'1_SPL;Sampling;Proteomics;Sample preparation;er:ISA;mod:1SPL',	0,	0);

DROP TABLE IF EXISTS `ProtocolXml`;
CREATE TABLE `ProtocolXml` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `FK_Name` varchar(512) NOT NULL,
  `XmlType` char(128) NOT NULL,
  `Xml` text NOT NULL,
  PRIMARY KEY (`ID`),
  KEY `Ind_FK_Name` (`FK_Name`),
  CONSTRAINT `ProtocolXml_ibfk_1` FOREIGN KEY (`FK_Name`) REFERENCES `Protocol` (`Name`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `ProtocolXml` (`ID`, `FK_Name`, `XmlType`, `Xml`) VALUES
(21,	'RNA-Seq Assay',	'TableXml',	'<?xml version=\"1.0\" ?><table displayName=\"annotationTable2\" id=\"4\" mc:Ignorable=\"xr xr3\" name=\"annotationTable2\" ref=\"A2:AV3\" totalsRowShown=\"0\" xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:xr=\"http://schemas.microsoft.com/office/spreadsheetml/2014/revision\" xmlns:xr3=\"http://schemas.microsoft.com/office/spreadsheetml/2016/revision3\" xr:uid=\"{644C39E7-36F0-9A4B-B432-FC158C4EAA9E}\"><autoFilter ref=\"A2:AV3\" xr:uid=\"{3BC338A9-04C5-5343-BA22-34CA5045E92A}\"/><tableColumns count=\"48\"><tableColumn id=\"1\" name=\"Source Name\" xr3:uid=\"{9ACF2691-D990-5D41-9C6B-275BFA06B6B8}\"/><tableColumn id=\"2\" name=\"Sample Name\" xr3:uid=\"{B871C6EF-90A3-1D4E-A537-D8FF5106BDBF}\"/><tableColumn dataDxfId=\"18\" id=\"3\" name=\"Parameter [Library strategy]\" xr3:uid=\"{94199463-8351-514E-9EAD-DFF200FA0E38}\"/><tableColumn id=\"4\" name=\"Term Source REF [Library strategy] (#h; #tNFDI4PSO:0000035)\" xr3:uid=\"{B1496354-2C5D-1C48-B0C5-B2D186BB2C34}\"/><tableColumn id=\"5\" name=\"Term Accession Number [Library strategy] (#h; #tNFDI4PSO:0000035)\" xr3:uid=\"{99D60052-24DF-8C41-A74E-AA5E1DF3D60B}\"/><tableColumn dataDxfId=\"17\" id=\"6\" name=\"Parameter [Library Selection]\" xr3:uid=\"{FC5523AE-20D6-9146-AE4D-5A54F4869A9D}\"/><tableColumn id=\"7\" name=\"Term Source REF [Library Selection] (#h; #tNFDI4PSO:0000036)\" xr3:uid=\"{39DA5E12-4B2B-8E43-A47E-CEF96F39EFD4}\"/><tableColumn id=\"8\" name=\"Term Accession Number [Library Selection] (#h; #tNFDI4PSO:0000036)\" xr3:uid=\"{B3C8EAFE-44F7-4247-B18F-EAD4B194CFE9}\"/><tableColumn dataDxfId=\"16\" id=\"9\" name=\"Parameter [Library layout]\" xr3:uid=\"{E47A58FD-232F-4440-8623-8F863EBE7722}\"/><tableColumn id=\"10\" name=\"Term Source REF [Library layout] (#h)\" xr3:uid=\"{7F27F67A-0619-9744-B635-28EA2E1EE2CA}\"/><tableColumn id=\"11\" name=\"Term Accession Number [Library layout] (#h)\" xr3:uid=\"{60293747-D5F7-E740-9296-E1E9B0006AC0}\"/><tableColumn dataDxfId=\"15\" id=\"15\" name=\"Parameter [Library preparation kit version]\" xr3:uid=\"{F4CB138B-D50D-AC40-AB47-F9127516E361}\"/><tableColumn id=\"16\" name=\"Term Source REF [Library preparation kit version] (#h; #tNFDI4PSO:0000038)\" xr3:uid=\"{F10584D2-46CF-5B44-B4D4-435967CF4AFF}\"/><tableColumn id=\"17\" name=\"Term Accession Number [Library preparation kit version] (#h; #tNFDI4PSO:0000038)\" xr3:uid=\"{C597DC15-81F1-C740-B52D-F69FAB2EA5E6}\"/><tableColumn dataDxfId=\"14\" id=\"12\" name=\"Parameter [Library preparation kit]\" xr3:uid=\"{1243D851-1C3F-494E-A25C-3B8BB8D0926B}\"/><tableColumn id=\"13\" name=\"Term Source REF [Library preparation kit] (#h; #tNFDI4PSO:0000037)\" xr3:uid=\"{8E3BDAF6-B635-B24A-A064-4E8E9A22A1D8}\"/><tableColumn id=\"14\" name=\"Term Accession Number [Library preparation kit] (#h; #tNFDI4PSO:0000037)\" xr3:uid=\"{E7CA87C9-F1BA-C441-A762-424DF654E451}\"/><tableColumn dataDxfId=\"13\" id=\"24\" name=\"Parameter [Adapter sequence]\" xr3:uid=\"{FC369AF1-57DB-7F44-9BF2-58D513DCFBBA}\"/><tableColumn id=\"25\" name=\"Term Source REF [Adapter sequence] (#h; #tNFDI4PSO:0000039)\" xr3:uid=\"{4DBE9ADE-00E1-BD41-925D-58DFFE98D935}\"/><tableColumn id=\"26\" name=\"Term Accession Number [Adapter sequence] (#h; #tNFDI4PSO:0000039)\" xr3:uid=\"{F4036B72-D4A4-7C48-A2F5-B6E6967B2009}\"/><tableColumn dataDxfId=\"12\" id=\"18\" name=\"Parameter [Library RNA amount]\" xr3:uid=\"{0CB5C37E-129C-8546-9BBA-9255AAB1830B}\"/><tableColumn id=\"19\" name=\"Term Source REF [Library RNA amount] (#h; #tNFDI4PSO:0000016)\" xr3:uid=\"{D350DE95-22D9-4C48-8FD4-7F17FE1B153B}\"/><tableColumn id=\"20\" name=\"Term Accession Number [Library RNA amount] (#h; #tNFDI4PSO:0000016)\" xr3:uid=\"{7E37E215-7AA7-3E45-BA96-B450C1E93B12}\"/><tableColumn id=\"36\" name=\"Unit [microgram] (#h; #tUO:0000023; #u)\" xr3:uid=\"{9CAC168C-0CD8-E24A-B25C-EDA4E42AA84E}\"/><tableColumn id=\"37\" name=\"Term Source REF [microgram] (#h; #tUO:0000023; #u)\" xr3:uid=\"{5AEAA556-05BD-FE4E-9E84-69BDB498DD20}\"/><tableColumn id=\"38\" name=\"Term Accession Number [microgram] (#h; #tUO:0000023; #u)\" xr3:uid=\"{842C4352-C24F-0644-8593-D8736BB88F8D}\"/><tableColumn dataDxfId=\"11\" id=\"27\" name=\"Parameter [Next generation sequencing instrument model]\" xr3:uid=\"{24CCEEDB-A0AF-684D-9D6D-7326D9EFB476}\"/><tableColumn id=\"28\" name=\"Term Source REF [Next generation sequencing instrument model] (#h; #tNFDI4PSO:0000040)\" xr3:uid=\"{2BFF580D-CCA0-0345-B808-5F5A1B3D3896}\"/><tableColumn id=\"29\" name=\"Term Accession Number [Next generation sequencing instrument model] (#h; #tNFDI4PSO:0000040)\" xr3:uid=\"{95D5E7F6-2884-B745-BDA8-798418A2BD07}\"/><tableColumn dataDxfId=\"10\" id=\"30\" name=\"Parameter [Base-calling Software]\" xr3:uid=\"{EA7A7326-1A7C-A846-8FBF-E381D349698F}\"/><tableColumn id=\"31\" name=\"Term Source REF [Base-calling Software] (#h; #tNFDI4PSO:0000017)\" xr3:uid=\"{7D7B7455-8E82-2B46-9BB8-CA37B80DB468}\"/><tableColumn id=\"32\" name=\"Term Accession Number [Base-calling Software] (#h; #tNFDI4PSO:0000017)\" xr3:uid=\"{EBDEFF6F-6B01-C54F-AF17-15C2AF95C7AB}\"/><tableColumn dataDxfId=\"9\" id=\"33\" name=\"Parameter [Base-calling Software Version]\" xr3:uid=\"{440360CA-BD83-A24F-ACCB-F08E4E07B309}\"/><tableColumn id=\"34\" name=\"Term Source REF [Base-calling Software Version] (#h; #tNFDI4PSO:0000018)\" xr3:uid=\"{910E1F5D-888F-094B-B509-1D3E5EAF20B2}\"/><tableColumn id=\"35\" name=\"Term Accession Number [Base-calling Software Version] (#h; #tNFDI4PSO:0000018)\" xr3:uid=\"{C3192EE8-6CFD-FA41-B093-05A3AFCD5131}\"/><tableColumn dataDxfId=\"8\" id=\"39\" name=\"Parameter [Base-calling Software Parameters]\" xr3:uid=\"{80C1A8E9-93E0-E447-BD7C-42D79D1FCA97}\"/><tableColumn id=\"40\" name=\"Term Source REF [Base-calling Software Parameters] (#h; #tNFDI4PSO:0000019)\" xr3:uid=\"{F919BED9-0D13-0D46-8D95-0BE92F377FAA}\"/><tableColumn id=\"41\" name=\"Term Accession Number [Base-calling Software Parameters] (#h; #tNFDI4PSO:0000019)\" xr3:uid=\"{4D29BD77-D990-7043-9498-70A782038238}\"/><tableColumn dataDxfId=\"7\" id=\"42\" name=\"Parameter [Library strand]\" xr3:uid=\"{5DEEB0AA-4BC3-F04E-ADAF-CB6EC27B6F47}\"/><tableColumn id=\"43\" name=\"Term Source REF [Library strand] (#h; #tNFDI4PSO:0000020)\" xr3:uid=\"{4A2942EF-C8A7-B447-A2D0-71C6825B7E67}\"/><tableColumn id=\"44\" name=\"Term Accession Number [Library strand] (#h; #tNFDI4PSO:0000020)\" xr3:uid=\"{F8E13DAD-A5E8-6043-93D9-AAF673CBB1AE}\"/><tableColumn dataDxfId=\"6\" id=\"45\" name=\"Data File Name\" xr3:uid=\"{6485F3EF-9735-B74E-958B-107722DC588D}\"/><tableColumn dataDxfId=\"5\" id=\"46\" name=\"Parameter [Raw data file format]\" xr3:uid=\"{0425BA52-9F21-3C43-9855-4DCF1AB9FDC8}\"/><tableColumn dataDxfId=\"4\" id=\"47\" name=\"Term Source REF [Raw data file format] (#h; #tNFDI4PSO:0000021)\" xr3:uid=\"{FFB5D601-D1A6-E247-9839-723F644EC700}\"/><tableColumn dataDxfId=\"3\" id=\"48\" name=\"Term Accession Number [Raw data file format] (#h; #tNFDI4PSO:0000021)\" xr3:uid=\"{C692B159-D0C9-534A-A34D-23F9298131E4}\"/><tableColumn dataDxfId=\"2\" id=\"49\" name=\"Parameter [Raw data file checksum]\" xr3:uid=\"{00B39407-5107-5447-86CF-D22F8056A82A}\"/><tableColumn dataDxfId=\"1\" id=\"50\" name=\"Term Source REF [Raw data file checksum] (#h; #tNFDI4PSO:0000022)\" xr3:uid=\"{4EC9F64B-15B4-AE42-8B72-B0FEA0C2E349}\"/><tableColumn dataDxfId=\"0\" id=\"51\" name=\"Term Accession Number [Raw data file checksum] (#h; #tNFDI4PSO:0000022)\" xr3:uid=\"{F5E8E62E-F8E6-814B-B57F-6C79EC78A7D1}\"/></tableColumns><tableStyleInfo name=\"TableStyleMedium7\" showColumnStripes=\"0\" showFirstColumn=\"0\" showLastColumn=\"0\" showRowStripes=\"1\"/></table>'),
(22,	'RNA-Seq Assay',	'CustomXml',	'<SwateTable Table=\"annotationTable2\" Worksheet=\"3ASY01_RNASeqGEO\"><TableValidation DateTime=\"2021-03-03 16:34\" SwateVersion=\"0.4.0\" TableName=\"annotationTable2\" Userlist=\"\" WorksheetName=\"3ASY01_RNASeqGEO\"><ColumnValidation ColumnAdress=\"0\" ColumnHeader=\"Source Name\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"1\" ColumnHeader=\"Sample Name\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"2\" ColumnHeader=\"Parameter [Library strategy]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm Library strategy\"/><ColumnValidation ColumnAdress=\"5\" ColumnHeader=\"Parameter [Library Selection]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm Library Selection\"/><ColumnValidation ColumnAdress=\"8\" ColumnHeader=\"Parameter [Library layout]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"11\" ColumnHeader=\"Parameter [Library preparation kit version]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"14\" ColumnHeader=\"Parameter [Library preparation kit]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"17\" ColumnHeader=\"Parameter [Adapter sequence]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"20\" ColumnHeader=\"Parameter [Library RNA amount]\" Importance=\"4\" Unit=\"microgram\" ValidationFormat=\"UnitTerm microgram\"/><ColumnValidation ColumnAdress=\"26\" ColumnHeader=\"Parameter [Next generation sequencing instrument model]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"29\" ColumnHeader=\"Parameter [Base-calling Software]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"32\" ColumnHeader=\"Parameter [Base-calling Software Version]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"35\" ColumnHeader=\"Parameter [Base-calling Software Parameters]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"38\" ColumnHeader=\"Parameter [Library strand]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"41\" ColumnHeader=\"Data File Name\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"42\" ColumnHeader=\"Parameter [Raw data file format]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"45\" ColumnHeader=\"Parameter [Raw data file checksum]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/></TableValidation></SwateTable>'),
(25,	'RNA-Seq Computational Analysis',	'TableXml',	'<?xml version=\"1.0\" ?><table displayName=\"annotationTable3\" id=\"5\" mc:Ignorable=\"xr xr3\" name=\"annotationTable3\" ref=\"A2:AF3\" totalsRowShown=\"0\" xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:xr=\"http://schemas.microsoft.com/office/spreadsheetml/2014/revision\" xmlns:xr3=\"http://schemas.microsoft.com/office/spreadsheetml/2016/revision3\" xr:uid=\"{B1E8DF05-C9A4-774C-927F-FE22EC52758A}\"><autoFilter ref=\"A2:AF3\" xr:uid=\"{856B7222-837D-994E-B57E-2C90FB73CB67}\"/><tableColumns count=\"32\"><tableColumn id=\"1\" name=\"Source Name\" xr3:uid=\"{FFCB64B6-F7E5-3141-8AC8-90DEF404CB27}\"/><tableColumn id=\"2\" name=\"Sample Name\" xr3:uid=\"{83E02E49-7422-0646-B55C-39A1A7410443}\"/><tableColumn dataDxfId=\"9\" id=\"4\" name=\"Parameter [Data filtering software]\" xr3:uid=\"{F9A5CAD2-0A38-694B-9DC8-DECACF24DB8E}\"/><tableColumn id=\"5\" name=\"Term Source REF [Data filtering software] (#h; #tNFDI4PSO:0000023)\" xr3:uid=\"{9BD7F665-ACF2-AC46-8E4B-BC65E4CC9332}\"/><tableColumn id=\"6\" name=\"Term Accession Number [Data filtering software] (#h; #tNFDI4PSO:0000023)\" xr3:uid=\"{F10506DB-3273-1241-9DB7-346C18CF37F1}\"/><tableColumn dataDxfId=\"8\" id=\"7\" name=\"Parameter [Data filtering software version]\" xr3:uid=\"{68487AE4-045B-704E-A2D6-A2A6C43E478B}\"/><tableColumn id=\"8\" name=\"Term Source REF [Data filtering software version] (#h; #tNFDI4PSO:0000024)\" xr3:uid=\"{A632703F-F636-4B4F-9F4D-62F900DD4347}\"/><tableColumn id=\"9\" name=\"Term Accession Number [Data filtering software version] (#h; #tNFDI4PSO:0000024)\" xr3:uid=\"{3FC0B085-3D78-6649-B099-338E88913144}\"/><tableColumn dataDxfId=\"7\" id=\"10\" name=\"Parameter [Data filtering Software Parameters]\" xr3:uid=\"{23307D4F-AFEA-5C4B-8BE1-C221DEE1C0DC}\"/><tableColumn id=\"11\" name=\"Term Source REF [Data filtering Software Parameters] (#h; #tNFDI4PSO:0000025)\" xr3:uid=\"{533A6307-FD87-4342-9FC3-3EE2FA1A5BD2}\"/><tableColumn id=\"12\" name=\"Term Accession Number [Data filtering Software Parameters] (#h; #tNFDI4PSO:0000025)\" xr3:uid=\"{9F20A65A-4044-5042-B538-01C73D1FF28A}\"/><tableColumn dataDxfId=\"6\" id=\"13\" name=\"Parameter [Read Alignment Software]\" xr3:uid=\"{17F4AAC9-26E6-A64B-87CC-F564323F95A2}\"/><tableColumn id=\"14\" name=\"Term Source REF [Read Alignment Software] (#h; #tNFDI4PSO:0000002)\" xr3:uid=\"{CB480599-32D5-3D47-8627-C13C12DD0846}\"/><tableColumn id=\"15\" name=\"Term Accession Number [Read Alignment Software] (#h; #tNFDI4PSO:0000002)\" xr3:uid=\"{2876D89B-FE92-2C4A-AA83-57EDA9956D37}\"/><tableColumn dataDxfId=\"5\" id=\"22\" name=\"Parameter [Read Alignment Software Version]\" xr3:uid=\"{7B135548-3E80-444F-A46D-623E886C73D6}\"/><tableColumn id=\"23\" name=\"Term Source REF [Read Alignment Software Version] (#h; #tNFDI4PSO:0000003)\" xr3:uid=\"{025F485C-08C4-AF40-9BF6-D462AAEA2325}\"/><tableColumn id=\"24\" name=\"Term Accession Number [Read Alignment Software Version] (#h; #tNFDI4PSO:0000003)\" xr3:uid=\"{3CA07897-AB36-F842-8796-DED31FD0F807}\"/><tableColumn dataDxfId=\"4\" id=\"25\" name=\"Parameter [Read Alignment Software Parameters]\" xr3:uid=\"{F90B0BDC-C22F-114D-AFC3-17D2DF0B1D22}\"/><tableColumn id=\"26\" name=\"Term Source REF [Read Alignment Software Parameters] (#h; #tNFDI4PSO:0000004)\" xr3:uid=\"{F09CEB55-616A-B84E-8DA8-AAAD529E7C2A}\"/><tableColumn id=\"27\" name=\"Term Accession Number [Read Alignment Software Parameters] (#h; #tNFDI4PSO:0000004)\" xr3:uid=\"{52F8AB65-BCD8-4F4A-8C7E-EDB7DEDD906C}\"/><tableColumn dataDxfId=\"3\" id=\"28\" name=\"Parameter [Genome reference sequence]\" xr3:uid=\"{E654730E-3E03-B744-962D-D0600B2531BE}\"/><tableColumn id=\"29\" name=\"Term Source REF [Genome reference sequence] (#h; #tNFDI4PSO:0000026)\" xr3:uid=\"{B3703F27-4802-1E40-9004-98F68F872BFA}\"/><tableColumn id=\"30\" name=\"Term Accession Number [Genome reference sequence] (#h; #tNFDI4PSO:0000026)\" xr3:uid=\"{EB347C09-2150-CA40-A1AC-873972D316C7}\"/><tableColumn dataDxfId=\"2\" id=\"31\" name=\"Parameter [Processed data file name]\" xr3:uid=\"{EBBCCD91-64AC-C946-AA91-3572672BB283}\"/><tableColumn id=\"32\" name=\"Term Source REF [Processed data file name] (#h; #tNFDI4PSO:0000028)\" xr3:uid=\"{3B8CDAC0-C510-4944-A64F-B74587C4D9D1}\"/><tableColumn id=\"33\" name=\"Term Accession Number [Processed data file name] (#h; #tNFDI4PSO:0000028)\" xr3:uid=\"{585BD5A3-841F-D341-87D6-7497347BD8DB}\"/><tableColumn dataDxfId=\"1\" id=\"34\" name=\"Parameter [Processed data file format]\" xr3:uid=\"{31F0606E-CA0F-2845-B598-91818C3E8DED}\"/><tableColumn id=\"35\" name=\"Term Source REF [Processed data file format] (#h; #tNFDI4PSO:0000027)\" xr3:uid=\"{2242708D-0E7B-4A4C-9438-B9769BD4A175}\"/><tableColumn id=\"36\" name=\"Term Accession Number [Processed data file format] (#h; #tNFDI4PSO:0000027)\" xr3:uid=\"{41E1DBD7-6CC8-3044-87D9-F4CBA8FAA96B}\"/><tableColumn dataDxfId=\"0\" id=\"37\" name=\"Parameter [Processed data file checksum]\" xr3:uid=\"{D6BE8D3D-1912-8545-81B9-972FD46CFB87}\"/><tableColumn id=\"38\" name=\"Term Source REF [Processed data file checksum] (#h; #tNFDI4PSO:0000029)\" xr3:uid=\"{6E3F9949-4270-4B40-BD63-3481325BCC9A}\"/><tableColumn id=\"39\" name=\"Term Accession Number [Processed data file checksum] (#h; #tNFDI4PSO:0000029)\" xr3:uid=\"{9C870A96-020A-C94A-979C-13940305A53C}\"/></tableColumns><tableStyleInfo name=\"TableStyleMedium7\" showColumnStripes=\"0\" showFirstColumn=\"0\" showLastColumn=\"0\" showRowStripes=\"1\"/></table>'),
(26,	'RNA-Seq Computational Analysis',	'CustomXml',	'<SwateTable Table=\"annotationTable3\" Worksheet=\"4COM01_RNASeqGEO\"><TableValidation DateTime=\"2021-03-11 15:14\" SwateVersion=\"0.4.4\" TableName=\"annotationTable3\" Userlist=\"\" WorksheetName=\"4COM01_RNASeqGEO\"><ColumnValidation ColumnAdress=\"0\" ColumnHeader=\"Source Name\" Importance=\"5\" Unit=\"None\" ValidationFormat=\"Text\"/><ColumnValidation ColumnAdress=\"1\" ColumnHeader=\"Sample Name\" Importance=\"5\" Unit=\"None\" ValidationFormat=\"Text\"/><ColumnValidation ColumnAdress=\"2\" ColumnHeader=\"Parameter [Data filtering software]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"5\" ColumnHeader=\"Parameter [Data filtering software version]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"8\" ColumnHeader=\"Parameter [Data filtering Software Parameters]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"11\" ColumnHeader=\"Parameter [Read Alignment Software]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"14\" ColumnHeader=\"Parameter [Read Alignment Software Version]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"17\" ColumnHeader=\"Parameter [Read Alignment Software Parameters]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"20\" ColumnHeader=\"Parameter [Genome reference sequence]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"23\" ColumnHeader=\"Parameter [Processed data file name]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"26\" ColumnHeader=\"Parameter [Processed data file format]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"29\" ColumnHeader=\"Parameter [Processed data file checksum]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/></TableValidation></SwateTable>'),
(27,	'Data Processing for Proteomics',	'TableXml',	'<?xml version=\"1.0\" ?><table displayName=\"annotationTable2\" id=\"3\" mc:Ignorable=\"xr xr3\" name=\"annotationTable2\" ref=\"A3:K4\" totalsRowShown=\"0\" xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:xr=\"http://schemas.microsoft.com/office/spreadsheetml/2014/revision\" xmlns:xr3=\"http://schemas.microsoft.com/office/spreadsheetml/2016/revision3\" xr:uid=\"{EA546D6D-2DD8-44CA-82F2-81D254FF18E0}\"><autoFilter ref=\"A3:K4\" xr:uid=\"{EAC4EAD3-60C7-4565-8D59-70225A9798C4}\"/><tableColumns count=\"11\"><tableColumn id=\"1\" name=\"Source Name\" xr3:uid=\"{32315100-2FE0-4D4B-A225-03D66FB82F9F}\"/><tableColumn dataDxfId=\"3\" id=\"3\" name=\"Parameter [acquisition software]\" xr3:uid=\"{713B6BA3-FB9A-4A0B-9109-F710AC918F90}\"/><tableColumn id=\"4\" name=\"Term Source REF [acquisition software] (#h; #tMS:1001455)\" xr3:uid=\"{B0D70AA5-D69B-469F-96B5-362991BF3D03}\"/><tableColumn id=\"5\" name=\"Term Accession Number [acquisition software] (#h; #tMS:1001455)\" xr3:uid=\"{DC1D9967-89D9-4877-BF03-29E895048A88}\"/><tableColumn dataDxfId=\"2\" id=\"6\" name=\"Parameter [analysis software]\" xr3:uid=\"{CFA20DDC-6B4B-4D7C-A69C-9EAB8C19A39D}\"/><tableColumn id=\"7\" name=\"Term Source REF [analysis software] (#h; #tMS:1001456)\" xr3:uid=\"{30824FDE-05AC-4F51-86CB-27BD56CC7074}\"/><tableColumn id=\"8\" name=\"Term Accession Number [analysis software] (#h; #tMS:1001456)\" xr3:uid=\"{53E6326F-F397-4B0E-851A-80F34DB42BBE}\"/><tableColumn dataDxfId=\"1\" id=\"9\" name=\"Parameter [data processing software]\" xr3:uid=\"{E47F58E2-6C0B-4357-9F65-3EF7494829C9}\"/><tableColumn id=\"10\" name=\"Term Source REF [data processing software] (#h; #tMS:1001457)\" xr3:uid=\"{71D84D5A-562B-4F94-9078-90B84E63201C}\"/><tableColumn id=\"11\" name=\"Term Accession Number [data processing software] (#h; #tMS:1001457)\" xr3:uid=\"{122941AE-EC84-4F86-8306-16CDC078F3AD}\"/><tableColumn dataDxfId=\"0\" id=\"12\" name=\"Data File Name\" xr3:uid=\"{A67370BE-3FC6-44B1-B6D5-05C90B87EBA6}\"/></tableColumns><tableStyleInfo name=\"TableStyleMedium7\" showColumnStripes=\"0\" showFirstColumn=\"0\" showLastColumn=\"0\" showRowStripes=\"1\"/></table>'),
(28,	'Data Processing for Proteomics',	'CustomXml',	'<SwateTable Table=\"annotationTable2\" Worksheet=\"Data procession\"><TableValidation DateTime=\"2021-03-02 14:11\" SwateVersion=\"0.4.0\" TableName=\"annotationTable2\" Userlist=\"\" WorksheetName=\"Data procession\"><ColumnValidation ColumnAdress=\"0\" ColumnHeader=\"Source Name\" Importance=\"5\" Unit=\"None\" ValidationFormat=\"Text\"/><ColumnValidation ColumnAdress=\"1\" ColumnHeader=\"Parameter [acquisition software]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm acquisition software\"/><ColumnValidation ColumnAdress=\"4\" ColumnHeader=\"Parameter [analysis software]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm analysis software\"/><ColumnValidation ColumnAdress=\"7\" ColumnHeader=\"Parameter [data processing software]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm data processing software\"/><ColumnValidation ColumnAdress=\"10\" ColumnHeader=\"Data File Name\" Importance=\"5\" Unit=\"None\" ValidationFormat=\"Text\"/></TableValidation></SwateTable>'),
(49,	'Sample Preparation for Proteomics',	'TableXml',	'<?xml version=\"1.0\" ?><table displayName=\"annotationTable\" id=\"1\" mc:Ignorable=\"xr xr3\" name=\"annotationTable\" ref=\"A3:AO4\" totalsRowShown=\"0\" xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:xr=\"http://schemas.microsoft.com/office/spreadsheetml/2014/revision\" xmlns:xr3=\"http://schemas.microsoft.com/office/spreadsheetml/2016/revision3\" xr:uid=\"{5B1F2463-857A-4A2C-ABB2-1CC90B0916AD}\"><autoFilter ref=\"A3:AO4\" xr:uid=\"{B7015486-3386-4FE9-96A2-D966D4D87A9A}\"/><tableColumns count=\"41\"><tableColumn id=\"1\" name=\"Source Name\" xr3:uid=\"{7C58FDB2-6A35-4FBD-A36A-FAA52B5D7CAF}\"/><tableColumn dataDxfId=\"11\" id=\"6\" name=\"Characteristics [species]\" xr3:uid=\"{BFCD02C1-6BFE-46D2-AD9F-2355E2C7D8AF}\"/><tableColumn id=\"7\" name=\"Term Source REF [species] (#h)\" xr3:uid=\"{BA506190-4D44-4563-AC44-A42C4030BC3E}\"/><tableColumn id=\"8\" name=\"Term Accession Number [species] (#h)\" xr3:uid=\"{32BB1AC1-8F2E-47E6-A832-0B0508750ED7}\"/><tableColumn dataDxfId=\"10\" id=\"9\" name=\"Characteristics [genotype information]\" xr3:uid=\"{92A0DDFF-25BE-40F9-B8F6-BA50F61FEBDB}\"/><tableColumn id=\"10\" name=\"Term Source REF [genotype information] (#h; #tOBI:0001305)\" xr3:uid=\"{3B645D8F-15B2-4E66-BABC-8334C534BB2D}\"/><tableColumn id=\"11\" name=\"Term Accession Number [genotype information] (#h; #tOBI:0001305)\" xr3:uid=\"{6DF01CA4-4C21-4D4D-88C7-A659D8D4D228}\"/><tableColumn dataDxfId=\"9\" id=\"12\" name=\"Characteristics [sample preparation]\" xr3:uid=\"{A18A441F-2A54-43C0-91AE-092AA1ADB726}\"/><tableColumn id=\"13\" name=\"Term Source REF [sample preparation] (#h; #tMS:1000831)\" xr3:uid=\"{0AECE118-0469-4F91-93C4-36C4A4364F2B}\"/><tableColumn id=\"14\" name=\"Term Accession Number [sample preparation] (#h; #tMS:1000831)\" xr3:uid=\"{476E9717-F9EB-4BD3-9BE4-AF4BBCE5087E}\"/><tableColumn dataDxfId=\"8\" id=\"15\" name=\"Characteristics [protein tag]\" xr3:uid=\"{9001C7D0-3FAF-45C4-A746-43FEE0829A56}\"/><tableColumn id=\"16\" name=\"Term Source REF [protein tag] (#h; #tGO:0031386)\" xr3:uid=\"{40894783-0D17-4678-AC32-5FDD2834AB91}\"/><tableColumn id=\"17\" name=\"Term Accession Number [protein tag] (#h; #tGO:0031386)\" xr3:uid=\"{28D7FEF6-A628-46CF-BE1A-9644730BD9FA}\"/><tableColumn dataDxfId=\"7\" id=\"18\" name=\"Characteristics [tissues, cell types and enzyme sources]\" xr3:uid=\"{46C9011F-B501-4BF9-B91A-026611A11EFA}\"/><tableColumn id=\"19\" name=\"Term Source REF [tissues, cell types and enzyme sources] (#h)\" xr3:uid=\"{C51BCFD2-4FB4-4DC2-82E1-C5519A1090A9}\"/><tableColumn id=\"20\" name=\"Term Accession Number [tissues, cell types and enzyme sources] (#h)\" xr3:uid=\"{5674935E-DAC9-4D27-97DA-583911B46660}\"/><tableColumn dataDxfId=\"6\" id=\"21\" name=\"Characteristics [cell]\" xr3:uid=\"{B6715B11-99FC-45B1-BCEC-371070ABCFA8}\"/><tableColumn id=\"22\" name=\"Term Source REF [cell] (#h)\" xr3:uid=\"{06B28DFC-E86F-49C9-9C58-58B0082409F8}\"/><tableColumn id=\"23\" name=\"Term Accession Number [cell] (#h)\" xr3:uid=\"{FFFD6BC1-095B-4AAF-86DC-6BDB1BAA9CC7}\"/><tableColumn dataDxfId=\"5\" id=\"24\" name=\"Characteristics [disease]\" xr3:uid=\"{A1405F5B-38A0-4D56-BC85-30A5310BB518}\"/><tableColumn id=\"25\" name=\"Term Source REF [disease] (#h)\" xr3:uid=\"{D63747A5-A57F-4B69-BFEA-669E020C315B}\"/><tableColumn id=\"26\" name=\"Term Accession Number [disease] (#h)\" xr3:uid=\"{050423D8-1E76-42ED-A375-526C37A40BC3}\"/><tableColumn dataDxfId=\"4\" id=\"27\" name=\"Characteristics [biological replicate]\" xr3:uid=\"{56322E71-9399-4F16-8198-F84548E0C213}\"/><tableColumn id=\"28\" name=\"Term Source REF [biological replicate] (#h; #tMS:1001809)\" xr3:uid=\"{44BC66ED-6D07-46F2-864B-7B589AEC4E80}\"/><tableColumn id=\"29\" name=\"Term Accession Number [biological replicate] (#h; #tMS:1001809)\" xr3:uid=\"{D0E1FCE1-7980-4288-AE35-9EF4FEBC057E}\"/><tableColumn dataDxfId=\"3\" id=\"33\" name=\"Parameter [spectrum interpretation]\" xr3:uid=\"{8F3FECF4-BB8F-47E1-B180-E3BE4ECC48B0}\"/><tableColumn id=\"34\" name=\"Term Source REF [spectrum interpretation] (#h; #tMS:1001000)\" xr3:uid=\"{129A59DC-A780-4F22-999D-7BEC5640F9D1}\"/><tableColumn id=\"35\" name=\"Term Accession Number [spectrum interpretation] (#h; #tMS:1001000)\" xr3:uid=\"{91FE757F-248B-44B5-AF1C-AA2A4B6D92AE}\"/><tableColumn dataDxfId=\"2\" id=\"36\" name=\"Parameter [matrix solution]\" xr3:uid=\"{97760BB8-73CE-4387-A8A6-4C2EA11C8F00}\"/><tableColumn id=\"37\" name=\"Term Source REF [matrix solution] (#h; #tMS:1000834)\" xr3:uid=\"{107A47D1-E4F9-4E41-88CD-AD778C4FCA1F}\"/><tableColumn id=\"38\" name=\"Term Accession Number [matrix solution] (#h; #tMS:1000834)\" xr3:uid=\"{A26060E6-F029-4EBA-93A5-A0B545994EF7}\"/><tableColumn dataDxfId=\"1\" id=\"72\" name=\"Parameter [temperature]\" xr3:uid=\"{5B589CF5-800D-4AF2-8EE7-8BE622688AF2}\"/><tableColumn id=\"73\" name=\"Term Source REF [temperature] (#h; #tPATO:000146)\" xr3:uid=\"{62EAFCC8-2800-4E49-81C6-33C6C154B470}\"/><tableColumn id=\"74\" name=\"Term Accession Number [temperature] (#h; #tPATO:000146)\" xr3:uid=\"{C1CB06C8-BF8D-4A96-AE85-1760D266B704}\"/><tableColumn id=\"3\" name=\"Unit [degree Celsius] (#h; #tUO:0000027; #u)\" xr3:uid=\"{53398734-FC16-45AC-9884-D6DC4CD6EA36}\"/><tableColumn id=\"4\" name=\"Term Source REF [degree Celsius] (#h; #tUO:0000027; #u)\" xr3:uid=\"{656DF6F9-FA68-44FE-BEE3-2B451B84E8C7}\"/><tableColumn id=\"5\" name=\"Term Accession Number [degree Celsius] (#h; #tUO:0000027; #u)\" xr3:uid=\"{025B1F9C-7EC6-46B2-AF23-5B711D236A0A}\"/><tableColumn dataDxfId=\"0\" id=\"69\" name=\"Parameter [time]\" xr3:uid=\"{6988B485-9F42-43CD-8BB9-2BFA30DB5425}\"/><tableColumn id=\"70\" name=\"Term Source REF [time] (#h; #tPATO:0000165)\" xr3:uid=\"{42E098B6-DBFD-4CAF-A878-BF6D15EA1BE7}\"/><tableColumn id=\"71\" name=\"Term Accession Number [time] (#h; #tPATO:0000165)\" xr3:uid=\"{22115B7D-348F-4A46-ACEC-F648AC971C5E}\"/><tableColumn id=\"2\" name=\"Sample Name\" xr3:uid=\"{5EBA6039-58E6-4A15-9FEE-7ACFCCB0CB15}\"/></tableColumns><tableStyleInfo name=\"TableStyleMedium7\" showColumnStripes=\"0\" showFirstColumn=\"0\" showLastColumn=\"0\" showRowStripes=\"1\"/></table>'),
(50,	'Sample Preparation for Proteomics',	'CustomXml',	'<SwateTable Table=\"annotationTable\" Worksheet=\"1SPL02_ProteomicsSamplePreparat\"><TableValidation DateTime=\"2021-03-11 19:45\" SwateVersion=\"0.4.4\" TableName=\"annotationTable\" Userlist=\"\" WorksheetName=\"1SPL02_ProteomicsSamplePreparat\"><ColumnValidation ColumnAdress=\"0\" ColumnHeader=\"Source Name\" Importance=\"5\" Unit=\"None\" ValidationFormat=\"Text\"/><ColumnValidation ColumnAdress=\"1\" ColumnHeader=\"Characteristics [species]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm species\"/><ColumnValidation ColumnAdress=\"4\" ColumnHeader=\"Characteristics [genotype information]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm genotype information\"/><ColumnValidation ColumnAdress=\"7\" ColumnHeader=\"Characteristics [sample preparation]\" Importance=\"5\" Unit=\"None\" ValidationFormat=\"OntologyTerm sample preparation\"/><ColumnValidation ColumnAdress=\"10\" ColumnHeader=\"Characteristics [protein tag]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm protein tag\"/><ColumnValidation ColumnAdress=\"13\" ColumnHeader=\"Characteristics [tissues, cell types and enzyme sources]\" Importance=\"2\" Unit=\"None\" ValidationFormat=\"OntologyTerm tissues, cell types and enzyme sources\"/><ColumnValidation ColumnAdress=\"16\" ColumnHeader=\"Characteristics [cell]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm cell\"/><ColumnValidation ColumnAdress=\"19\" ColumnHeader=\"Characteristics [disease]\" Importance=\"1\" Unit=\"None\" ValidationFormat=\"OntologyTerm disease\"/><ColumnValidation ColumnAdress=\"22\" ColumnHeader=\"Characteristics [biological replicate]\" Importance=\"2\" Unit=\"None\" ValidationFormat=\"Int\"/><ColumnValidation ColumnAdress=\"25\" ColumnHeader=\"Parameter [spectrum interpretation]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm spectrum interpretation\"/><ColumnValidation ColumnAdress=\"28\" ColumnHeader=\"Parameter [matrix solution]\" Importance=\"2\" Unit=\"None\" ValidationFormat=\"OntologyTerm matrix solution\"/><ColumnValidation ColumnAdress=\"31\" ColumnHeader=\"Parameter [temperature]\" Importance=\"3\" Unit=\"degree Celsius\" ValidationFormat=\"UnitTerm degree Celsius\"/><ColumnValidation ColumnAdress=\"37\" ColumnHeader=\"Parameter [time]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"Number\"/><ColumnValidation ColumnAdress=\"40\" ColumnHeader=\"Sample Name\" Importance=\"5\" Unit=\"None\" ValidationFormat=\"Text\"/></TableValidation></SwateTable>'),
(51,	'Extraction for Proteomics',	'TableXml',	'<?xml version=\"1.0\" ?><table displayName=\"annotationTable\" id=\"4\" mc:Ignorable=\"xr xr3\" name=\"annotationTable\" ref=\"A3:AF4\" totalsRowShown=\"0\" xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:xr=\"http://schemas.microsoft.com/office/spreadsheetml/2014/revision\" xmlns:xr3=\"http://schemas.microsoft.com/office/spreadsheetml/2016/revision3\" xr:uid=\"{6D6A5A68-C33B-42E1-9BA0-0CF433F987F9}\"><autoFilter ref=\"A3:AF4\" xr:uid=\"{9ED13356-DBC8-4ADA-9EB3-EC237093C08B}\"/><tableColumns count=\"32\"><tableColumn id=\"1\" name=\"Source Name\" xr3:uid=\"{6C2FF147-5B29-4B17-88EA-BB5AFE8A67F6}\"/><tableColumn dataDxfId=\"8\" id=\"3\" name=\"Parameter [Quantification method]\" xr3:uid=\"{B2F1C7F6-AFC6-4E93-85E7-85E99E5671B4}\"/><tableColumn id=\"4\" name=\"Term Source REF [Quantification method] (#h)\" xr3:uid=\"{E025D978-A587-469A-A963-DBE1D46F5A9F}\"/><tableColumn id=\"5\" name=\"Term Accession Number [Quantification method] (#h)\" xr3:uid=\"{5895A0FA-6633-41BB-92C4-4162FBD4FBFB}\"/><tableColumn dataDxfId=\"7\" id=\"6\" name=\"Parameter [cleavage agent name]\" xr3:uid=\"{92AFA76A-0D44-4C11-B7FB-1C775EAFCA47}\"/><tableColumn id=\"7\" name=\"Term Source REF [cleavage agent name] (#h; #tMS:1001045)\" xr3:uid=\"{4206E4F6-AD8B-4BB1-96B9-0F47919A5941}\"/><tableColumn id=\"8\" name=\"Term Accession Number [cleavage agent name] (#h; #tMS:1001045)\" xr3:uid=\"{B8B0F3B4-8C6B-4F86-BD67-3076AD1A934B}\"/><tableColumn dataDxfId=\"6\" id=\"9\" name=\"Parameter [molecule]\" xr3:uid=\"{FB4E1F5F-7505-49DF-885E-3C1C54A061A5}\"/><tableColumn id=\"10\" name=\"Term Source REF [molecule] (#h; #tMS:1000859)\" xr3:uid=\"{237E465E-8608-4174-B813-37AAE992F358}\"/><tableColumn id=\"11\" name=\"Term Accession Number [molecule] (#h; #tMS:1000859)\" xr3:uid=\"{6D0D8618-C2F5-4236-9590-1375F3A009A4}\"/><tableColumn dataDxfId=\"5\" id=\"12\" name=\"Parameter [sample state]\" xr3:uid=\"{2EF6ED0C-33E6-47B5-A088-998A67C1912E}\"/><tableColumn id=\"13\" name=\"Term Source REF [sample state] (#h; #tMS:1000003)\" xr3:uid=\"{3EFCB430-F747-49C4-8423-F888B9867790}\"/><tableColumn id=\"14\" name=\"Term Accession Number [sample state] (#h; #tMS:1000003)\" xr3:uid=\"{C169AF91-D8E2-46AE-A598-D62E43F6454F}\"/><tableColumn dataDxfId=\"4\" id=\"15\" name=\"Parameter [staining]\" xr3:uid=\"{13237B93-8772-46A8-98B2-2C9B4E8BE6C9}\"/><tableColumn id=\"16\" name=\"Term Source REF [staining] (#h; #tOBI:0302887)\" xr3:uid=\"{A9DD2802-7F6F-46B0-A57E-3871BB5C33B9}\"/><tableColumn id=\"17\" name=\"Term Accession Number [staining] (#h; #tOBI:0302887)\" xr3:uid=\"{4683F25B-E068-4D92-8BBB-59B944EDDDB8}\"/><tableColumn dataDxfId=\"3\" id=\"18\" name=\"Parameter [buffer]\" xr3:uid=\"{7B2184D8-7AE3-47CC-ADED-9EC395AA8051}\"/><tableColumn id=\"19\" name=\"Term Source REF [buffer] (#h; #tCHEBI:35225)\" xr3:uid=\"{972B1E7A-67C5-46C9-A3A5-CD7F1B80485C}\"/><tableColumn id=\"20\" name=\"Term Accession Number [buffer] (#h; #tCHEBI:35225)\" xr3:uid=\"{8191BFA8-CDDE-430A-AF3E-B23D09DA7E63}\"/><tableColumn dataDxfId=\"2\" id=\"27\" name=\"Parameter [pH]\" xr3:uid=\"{D25D2D96-84C2-4960-9F80-5F730D299AA2}\"/><tableColumn id=\"28\" name=\"Term Source REF [pH] (#h; #tUO:0000196)\" xr3:uid=\"{CB9FC1F8-3EFD-469E-86D3-65EFC904BA6A}\"/><tableColumn id=\"29\" name=\"Term Accession Number [pH] (#h; #tUO:0000196)\" xr3:uid=\"{899E2937-A95F-4C9E-B7AC-A52A2CF1A5A0}\"/><tableColumn id=\"30\" name=\"Unit [pH] (#h; #tUO:0000196; #u)\" xr3:uid=\"{204A2F5E-5729-4C5D-8CC6-E885CF92317A}\"/><tableColumn id=\"31\" name=\"Term Source REF [pH] (#h; #tUO:0000196; #u)\" xr3:uid=\"{1C1368AC-93DA-4021-8548-2DC72285FF72}\"/><tableColumn id=\"32\" name=\"Term Accession Number [pH] (#h; #tUO:0000196; #u)\" xr3:uid=\"{068B3A78-E884-4716-9EF7-6482CF72BEEE}\"/><tableColumn dataDxfId=\"1\" id=\"33\" name=\"Parameter [sample pre-fractionation]\" xr3:uid=\"{F7AF8144-CBA9-4923-98E8-DC230D793FA3}\"/><tableColumn id=\"34\" name=\"Term Source REF [sample pre-fractionation] (#h; #tMS:1002493)\" xr3:uid=\"{B0C80671-6F9E-4835-8FE0-FDAA27DFBDB2}\"/><tableColumn id=\"35\" name=\"Term Accession Number [sample pre-fractionation] (#h; #tMS:1002493)\" xr3:uid=\"{ECF77BF2-142C-4EAA-9BA5-BB721FB79791}\"/><tableColumn dataDxfId=\"0\" id=\"21\" name=\"Parameter [protein column]\" xr3:uid=\"{D4E1879C-4299-4909-AFFD-548C0019F3EB}\"/><tableColumn id=\"22\" name=\"Term Source REF [protein column] (#h; #tOBI:0000468)\" xr3:uid=\"{E48751B1-B0D7-43F4-A81A-62A50B13FDB2}\"/><tableColumn id=\"23\" name=\"Term Accession Number [protein column] (#h; #tOBI:0000468)\" xr3:uid=\"{CF3BD891-A74C-45FC-B66B-35173F7EB05F}\"/><tableColumn id=\"2\" name=\"Sample Name\" xr3:uid=\"{DA0C991B-2298-40BE-B7C2-5CB21925B996}\"/></tableColumns><tableStyleInfo name=\"TableStyleMedium7\" showColumnStripes=\"0\" showFirstColumn=\"0\" showLastColumn=\"0\" showRowStripes=\"1\"/></table>'),
(52,	'Extraction for Proteomics',	'CustomXml',	'<SwateTable Table=\"annotationTable\" Worksheet=\"2EXT02_ProteomicsExtraction\"><TableValidation DateTime=\"2021-03-12 03:09\" SwateVersion=\"0.4.4\" TableName=\"annotationTable\" Userlist=\"\" WorksheetName=\"2EXT02_ProteomicsExtraction\"><ColumnValidation ColumnAdress=\"0\" ColumnHeader=\"Source Name\" Importance=\"5\" Unit=\"None\" ValidationFormat=\"Text\"/><ColumnValidation ColumnAdress=\"1\" ColumnHeader=\"Parameter [Quantification method]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm Quantification method\"/><ColumnValidation ColumnAdress=\"4\" ColumnHeader=\"Parameter [cleavage agent name]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm cleavage agent name\"/><ColumnValidation ColumnAdress=\"7\" ColumnHeader=\"Parameter [molecule]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm molecule\"/><ColumnValidation ColumnAdress=\"10\" ColumnHeader=\"Parameter [sample state]\" Importance=\"2\" Unit=\"None\" ValidationFormat=\"OntologyTerm sample state\"/><ColumnValidation ColumnAdress=\"13\" ColumnHeader=\"Parameter [staining]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm staining\"/><ColumnValidation ColumnAdress=\"16\" ColumnHeader=\"Parameter [buffer]\" Importance=\"2\" Unit=\"None\" ValidationFormat=\"OntologyTerm buffer\"/><ColumnValidation ColumnAdress=\"19\" ColumnHeader=\"Parameter [pH]\" Importance=\"2\" Unit=\"pH\" ValidationFormat=\"UnitTerm pH\"/><ColumnValidation ColumnAdress=\"25\" ColumnHeader=\"Parameter [sample pre-fractionation]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm sample pre-fractionation\"/><ColumnValidation ColumnAdress=\"28\" ColumnHeader=\"Parameter [protein column]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm protein column\"/><ColumnValidation ColumnAdress=\"31\" ColumnHeader=\"Sample Name\" Importance=\"5\" Unit=\"None\" ValidationFormat=\"Text\"/></TableValidation></SwateTable>'),
(53,	'Measurement for Proteomics',	'TableXml',	'<?xml version=\"1.0\" ?><table displayName=\"annotationTable\" id=\"2\" mc:Ignorable=\"xr xr3\" name=\"annotationTable\" ref=\"A3:Z4\" totalsRowShown=\"0\" xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:xr=\"http://schemas.microsoft.com/office/spreadsheetml/2014/revision\" xmlns:xr3=\"http://schemas.microsoft.com/office/spreadsheetml/2016/revision3\" xr:uid=\"{182571E2-08C5-4CDD-BC87-4140A0F2C8FE}\"><autoFilter ref=\"A3:Z4\" xr:uid=\"{45A12931-8675-4B3D-BB5E-DBF348EF23A8}\"/><tableColumns count=\"26\"><tableColumn id=\"1\" name=\"Source Name\" xr3:uid=\"{A8E7EE3D-FBD8-4EC1-BEF2-C438C95B647C}\"/><tableColumn dataDxfId=\"8\" id=\"3\" name=\"Parameter [technical replicate]\" xr3:uid=\"{C1B561A6-C828-4049-BDEA-AF3E1C136B19}\"/><tableColumn id=\"4\" name=\"Term Source REF [technical replicate] (#h; #tMS:1001808)\" xr3:uid=\"{0632C6FA-2781-4C57-B39C-D57A7D8D1AF5}\"/><tableColumn id=\"5\" name=\"Term Accession Number [technical replicate] (#h; #tMS:1001808)\" xr3:uid=\"{66050253-4C1E-4C61-AB9E-8BAF88D20441}\"/><tableColumn dataDxfId=\"7\" id=\"6\" name=\"Parameter [Variable modification]\" xr3:uid=\"{050E2CF6-315B-4B3A-B97D-5EEBB4E7A38C}\"/><tableColumn id=\"7\" name=\"Term Source REF [Variable modification] (#h)\" xr3:uid=\"{0FA497D4-1C95-4C2E-B9DD-A8812A7B3E9B}\"/><tableColumn id=\"8\" name=\"Term Accession Number [Variable modification] (#h)\" xr3:uid=\"{96EC72B1-2B0C-4B1D-A677-69DA16A09C1D}\"/><tableColumn dataDxfId=\"6\" id=\"9\" name=\"Parameter [Fixed modification]\" xr3:uid=\"{ACE38909-58D7-441D-919F-846224465A4A}\"/><tableColumn id=\"10\" name=\"Term Source REF [Fixed modification] (#h)\" xr3:uid=\"{DD93C755-4DBB-4D60-919F-D4CC5AEC2E85}\"/><tableColumn id=\"11\" name=\"Term Accession Number [Fixed modification] (#h)\" xr3:uid=\"{A5A23A0C-DB75-4DC4-9436-6EE1BE4D92EC}\"/><tableColumn dataDxfId=\"5\" id=\"12\" name=\"Parameter [sample volume]\" xr3:uid=\"{978870C3-45B8-4ECB-B2CA-86D03BF2A698}\"/><tableColumn id=\"13\" name=\"Term Source REF [sample volume] (#h; #tMS:1000005)\" xr3:uid=\"{AF32B7DD-7A4B-45D6-B950-11FE6503A2E6}\"/><tableColumn id=\"14\" name=\"Term Accession Number [sample volume] (#h; #tMS:1000005)\" xr3:uid=\"{BF76EE53-451D-45CC-B72B-91999CC5BD69}\"/><tableColumn dataDxfId=\"4\" id=\"15\" name=\"Parameter [injection volume]\" xr3:uid=\"{8E199218-2D70-4928-83D2-0493C69C3BCB}\"/><tableColumn id=\"16\" name=\"Term Source REF [injection volume] (#h)\" xr3:uid=\"{06A4B0BA-1B41-4FBC-8D68-118BEEDD8296}\"/><tableColumn id=\"17\" name=\"Term Accession Number [injection volume] (#h)\" xr3:uid=\"{3CC97A7D-5763-4138-AAA9-D22A7535659E}\"/><tableColumn dataDxfId=\"3\" id=\"18\" name=\"Parameter [count unit]\" xr3:uid=\"{A3C32411-FEB3-4D4F-8ED2-92E7FD7803B7}\"/><tableColumn id=\"19\" name=\"Term Source REF [count unit] (#h; #tUO:0000189)\" xr3:uid=\"{ED2F4028-BFB3-4F83-96F5-FAE193DB615C}\"/><tableColumn id=\"20\" name=\"Term Accession Number [count unit] (#h; #tUO:0000189)\" xr3:uid=\"{2E98C911-7E03-4FA1-9EFB-B04CD1384E29}\"/><tableColumn dataDxfId=\"2\" id=\"21\" name=\"Parameter [instrument model]\" xr3:uid=\"{B7ECBE5F-1E69-4C1D-B18D-7EEE84832D19}\"/><tableColumn id=\"22\" name=\"Term Source REF [instrument model] (#h; #tMS:1000031)\" xr3:uid=\"{930F4078-7111-4DCF-8248-4593E559C601}\"/><tableColumn id=\"23\" name=\"Term Accession Number [instrument model] (#h; #tMS:1000031)\" xr3:uid=\"{F368466C-6F70-442F-839A-2630E09F537E}\"/><tableColumn dataDxfId=\"1\" id=\"24\" name=\"Parameter [duration]\" xr3:uid=\"{9297F5B7-8ED5-42A4-876B-DB810A3F215C}\"/><tableColumn id=\"25\" name=\"Term Source REF [duration] (#h; #tPATO:0001309)\" xr3:uid=\"{4A98B15A-B099-46B8-BF60-3565FAF49E82}\"/><tableColumn id=\"26\" name=\"Term Accession Number [duration] (#h; #tPATO:0001309)\" xr3:uid=\"{1C577371-0591-4444-AEBD-BDA54BCBE616}\"/><tableColumn dataDxfId=\"0\" id=\"27\" name=\"Data File Name\" xr3:uid=\"{E7C1594F-90F7-4531-95B2-7963F3B2F7E5}\"/></tableColumns><tableStyleInfo name=\"TableStyleMedium7\" showColumnStripes=\"0\" showFirstColumn=\"0\" showLastColumn=\"0\" showRowStripes=\"1\"/></table>'),
(54,	'Measurement for Proteomics',	'CustomXml',	'<SwateTable Table=\"annotationTable\" Worksheet=\"3ASY02_ProteomicsMeasurement\"><TableValidation DateTime=\"2021-03-02 14:10\" SwateVersion=\"0.4.4\" TableName=\"annotationTable\" Userlist=\"\" WorksheetName=\"3ASY02_ProteomicsMeasurement\"><ColumnValidation ColumnAdress=\"0\" ColumnHeader=\"Source Name\" Importance=\"5\" Unit=\"None\" ValidationFormat=\"Text\"/><ColumnValidation ColumnAdress=\"1\" ColumnHeader=\"Parameter [technical replicate]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"Int\"/><ColumnValidation ColumnAdress=\"4\" ColumnHeader=\"Parameter [Variable modification]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm Variable modification\"/><ColumnValidation ColumnAdress=\"7\" ColumnHeader=\"Parameter [Fixed modification]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm Fixed modification\"/><ColumnValidation ColumnAdress=\"10\" ColumnHeader=\"Parameter [sample volume]\" Importance=\"2\" Unit=\"None\" ValidationFormat=\"Number\"/><ColumnValidation ColumnAdress=\"13\" ColumnHeader=\"Parameter [injection volume]\" Importance=\"2\" Unit=\"None\" ValidationFormat=\"Number\"/><ColumnValidation ColumnAdress=\"16\" ColumnHeader=\"Parameter [count unit]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"Int\"/><ColumnValidation ColumnAdress=\"19\" ColumnHeader=\"Parameter [instrument model]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm instrument model\"/><ColumnValidation ColumnAdress=\"22\" ColumnHeader=\"Parameter [duration]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"Number\"/><ColumnValidation ColumnAdress=\"25\" ColumnHeader=\"Data File Name\" Importance=\"5\" Unit=\"None\" ValidationFormat=\"Text\"/></TableValidation></SwateTable>'),
(61,	'Plant growth',	'TableXml',	'<?xml version=\"1.0\" ?><table displayName=\"annotationTable\" id=\"2\" mc:Ignorable=\"xr xr3\" name=\"annotationTable\" ref=\"A2:CE3\" totalsRowShown=\"0\" xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:xr=\"http://schemas.microsoft.com/office/spreadsheetml/2014/revision\" xmlns:xr3=\"http://schemas.microsoft.com/office/spreadsheetml/2016/revision3\" xr:uid=\"{A7A0FDF1-D7A7-5444-9FF6-B24A8DE79F17}\"><autoFilter ref=\"A2:CE3\" xr:uid=\"{8A7F28C9-E08B-8A4D-99FD-3313667921ED}\"/><tableColumns count=\"83\"><tableColumn id=\"1\" name=\"Source Name\" xr3:uid=\"{9B8F9057-2F20-7A45-8C27-9A36422B26D5}\"/><tableColumn id=\"2\" name=\"Sample Name\" xr3:uid=\"{CC93B5FA-49F4-9B4C-B1F6-452471229F30}\"/><tableColumn dataDxfId=\"21\" id=\"72\" name=\"Characteristics [Biological replicate]\" xr3:uid=\"{A33FCD3B-AD29-A742-9D63-6ECAFBEE068E}\"/><tableColumn id=\"73\" name=\"Term Source REF [Biological replicate] (#h; #tNFDI4PSO:0000042)\" xr3:uid=\"{4059FECC-FE9D-5947-9219-570196389DAE}\"/><tableColumn id=\"74\" name=\"Term Accession Number [Biological replicate] (#h; #tNFDI4PSO:0000042)\" xr3:uid=\"{E04811ED-EF58-AD4F-84F5-156D9251E904}\"/><tableColumn dataDxfId=\"20\" id=\"3\" name=\"Characteristics [Organism]\" xr3:uid=\"{6A7FF2B3-5F1E-8245-AC4F-6198097A8123}\"/><tableColumn id=\"4\" name=\"Term Source REF [Organism] (#h; #tNFDI4PSO:0000030)\" xr3:uid=\"{CC831F2C-80A0-C84F-829F-23F6C78F2EEB}\"/><tableColumn id=\"5\" name=\"Term Accession Number [Organism] (#h; #tNFDI4PSO:0000030)\" xr3:uid=\"{D6E59B6D-D53F-A840-A182-24AE5A460E1D}\"/><tableColumn dataDxfId=\"19\" id=\"14\" name=\"Characteristics [Genotype]\" xr3:uid=\"{000DCCE3-5EF7-4F4E-A82A-AD088FF36730}\"/><tableColumn id=\"15\" name=\"Term Source REF [Genotype] (#h; #tNFDI4PSO:0000031)\" xr3:uid=\"{E2A1AEEB-EF2B-F448-824C-2953C18CC24A}\"/><tableColumn id=\"16\" name=\"Term Accession Number [Genotype] (#h; #tNFDI4PSO:0000031)\" xr3:uid=\"{A998FE5F-8994-6E40-94FA-F0EF6DC905E4}\"/><tableColumn dataDxfId=\"18\" id=\"17\" name=\"Characteristics [Organism part]\" xr3:uid=\"{0EB867DC-9BB4-6243-BEE9-01764D9FEB35}\"/><tableColumn id=\"18\" name=\"Term Source REF [Organism part] (#h; #tNFDI4PSO:0000032)\" xr3:uid=\"{B0CCF08F-B694-6C4F-94E6-E17AFD4C0A63}\"/><tableColumn id=\"19\" name=\"Term Accession Number [Organism part] (#h; #tNFDI4PSO:0000032)\" xr3:uid=\"{6A02A31E-2B72-BE41-BF86-71E587B29D13}\"/><tableColumn dataDxfId=\"17\" id=\"20\" name=\"Characteristics [age]\" xr3:uid=\"{96C34AC4-65C0-774B-A5A0-1BC98C955028}\"/><tableColumn id=\"21\" name=\"Term Source REF [age] (#h; #tNFDI4PSO:0000033)\" xr3:uid=\"{325D3F43-A306-2F47-AD9C-92F61C0649E5}\"/><tableColumn id=\"22\" name=\"Term Accession Number [age] (#h; #tNFDI4PSO:0000033)\" xr3:uid=\"{334AF647-E268-2E41-950D-8973A43E29C8}\"/><tableColumn dataDxfId=\"16\" id=\"26\" name=\"Characteristics [study type]\" xr3:uid=\"{5E219BCA-059A-CC41-93BF-8CD61AA9572D}\"/><tableColumn id=\"27\" name=\"Term Source REF [study type] (#h; #tPECO:0007231)\" xr3:uid=\"{5FF2BBBF-350F-9842-8FDF-2F895B07EADF}\"/><tableColumn id=\"28\" name=\"Term Accession Number [study type] (#h; #tPECO:0007231)\" xr3:uid=\"{E12790EC-754D-3244-8A7A-1242F7942612}\"/><tableColumn dataDxfId=\"15\" id=\"23\" name=\"Characteristics [plant growth medium exposure]\" xr3:uid=\"{E0A293FE-1EF5-A949-9C12-ADF40314B418}\"/><tableColumn id=\"24\" name=\"Term Source REF [plant growth medium exposure] (#h; #tPECO:0007147)\" xr3:uid=\"{0C19F4A6-F9E8-1F4F-9B61-EB31E6DF451A}\"/><tableColumn id=\"25\" name=\"Term Accession Number [plant growth medium exposure] (#h; #tPECO:0007147)\" xr3:uid=\"{7BBED07D-F814-A245-B090-723342B3D631}\"/><tableColumn dataDxfId=\"14\" id=\"29\" name=\"Characteristics [growth plot design]\" xr3:uid=\"{C386EF1A-F4F8-C543-99A0-ABD311BD175F}\"/><tableColumn id=\"30\" name=\"Term Source REF [growth plot design] (#h; #tNFDI4PSO:0000001)\" xr3:uid=\"{86FAE20B-4F89-EE4F-91E6-CE3B7E712F95}\"/><tableColumn id=\"31\" name=\"Term Accession Number [growth plot design] (#h; #tNFDI4PSO:0000001)\" xr3:uid=\"{C0949892-595C-9742-BCD1-75134FAC012F}\"/><tableColumn dataDxfId=\"13\" id=\"32\" name=\"Characteristics [Growth day length]\" xr3:uid=\"{B90A4652-69D5-2E4F-8A64-A3A5A5CB24DB}\"/><tableColumn id=\"33\" name=\"Term Source REF [Growth day length] (#h; #tNFDI4PSO:0000041)\" xr3:uid=\"{4E2448E1-3D08-504C-8FE3-D71B801F4DC9}\"/><tableColumn id=\"34\" name=\"Term Accession Number [Growth day length] (#h; #tNFDI4PSO:0000041)\" xr3:uid=\"{D47410BE-0000-B648-8CE5-966EE8968909}\"/><tableColumn dataDxfId=\"12\" id=\"35\" name=\"Characteristics [light intensity exposure]\" xr3:uid=\"{27034924-8879-044A-819B-7DC832A9D603}\"/><tableColumn id=\"36\" name=\"Term Source REF [light intensity exposure] (#h; #tPECO:0007224)\" xr3:uid=\"{FD04748E-7F58-4A46-A167-E5EB1375A50B}\"/><tableColumn id=\"37\" name=\"Term Accession Number [light intensity exposure] (#h; #tPECO:0007224)\" xr3:uid=\"{D76E6422-DF20-B94A-B5A6-4FD7F7923EBF}\"/><tableColumn id=\"6\" name=\"Unit [microeinstein per square meter per second] (#h; #tUO:0000160; #u)\" xr3:uid=\"{8E446DD1-817A-E84A-9536-47B88B72BFD8}\"/><tableColumn id=\"7\" name=\"Term Source REF [microeinstein per square meter per second] (#h; #tUO:0000160; #u)\" xr3:uid=\"{BCB29467-6D87-F141-98A0-60243765731F}\"/><tableColumn id=\"8\" name=\"Term Accession Number [microeinstein per square meter per second] (#h; #tUO:0000160; #u)\" xr3:uid=\"{D2565EAD-7C86-C343-800D-E37FD463093B}\"/><tableColumn dataDxfId=\"11\" id=\"38\" name=\"Characteristics [Humidity Day]\" xr3:uid=\"{21746D48-E9A2-5B42-BC00-F5AFD4793AA7}\"/><tableColumn id=\"39\" name=\"Term Source REF [Humidity Day] (#h; #tNFDI4PSO:0000005)\" xr3:uid=\"{8B1D9CC4-6026-674C-9852-06A06365907B}\"/><tableColumn id=\"40\" name=\"Term Accession Number [Humidity Day] (#h; #tNFDI4PSO:0000005)\" xr3:uid=\"{8CCA0026-8267-884E-8F9A-369ACCB825EA}\"/><tableColumn id=\"9\" name=\"Unit [percent] (#h; #tUO:0000187; #u)\" xr3:uid=\"{9508FD23-5388-F64E-8286-05B533856CA2}\"/><tableColumn id=\"10\" name=\"Term Source REF [percent] (#h; #tUO:0000187; #u)\" xr3:uid=\"{F796BC22-CBE1-6D41-824E-F655C3F9D5B6}\"/><tableColumn id=\"11\" name=\"Term Accession Number [percent] (#h; #tUO:0000187; #u)\" xr3:uid=\"{32AC3DD8-82CD-4349-BFD5-0805A34144D8}\"/><tableColumn dataDxfId=\"10\" id=\"41\" name=\"Characteristics [Humidity Night]\" xr3:uid=\"{F4E42B79-6B62-E34C-95BE-8906E80BFBC8}\"/><tableColumn id=\"42\" name=\"Term Source REF [Humidity Night] (#h; #tNFDI4PSO:0000006)\" xr3:uid=\"{FE26FC08-E3D1-7547-8E20-1363EF47EE86}\"/><tableColumn id=\"43\" name=\"Term Accession Number [Humidity Night] (#h; #tNFDI4PSO:0000006)\" xr3:uid=\"{C6D20AF0-60A8-874E-9614-2A2F483EB262}\"/><tableColumn id=\"12\" name=\"Unit [percent] (#2; #h; #tUO:0000187; #u)\" xr3:uid=\"{2866C2CF-81B3-694E-B2D9-F69059F54AA2}\"/><tableColumn id=\"13\" name=\"Term Source REF [percent] (#2; #h; #tUO:0000187; #u)\" xr3:uid=\"{A885FE47-7729-3B45-B73D-03FF1F114CD5}\"/><tableColumn id=\"65\" name=\"Term Accession Number [percent] (#2; #h; #tUO:0000187; #u)\" xr3:uid=\"{A3AEBB25-5500-F249-866E-C98439259F17}\"/><tableColumn dataDxfId=\"9\" id=\"44\" name=\"Characteristics [Temperature Day]\" xr3:uid=\"{F0571376-132B-F940-9609-6B59D9845B1B}\"/><tableColumn id=\"45\" name=\"Term Source REF [Temperature Day] (#h; #tNFDI4PSO:0000007)\" xr3:uid=\"{23233498-CFC4-9F46-94D5-39E7A6B5AFB5}\"/><tableColumn id=\"46\" name=\"Term Accession Number [Temperature Day] (#h; #tNFDI4PSO:0000007)\" xr3:uid=\"{346D483D-D0DE-5140-A015-4AD0367BAD4D}\"/><tableColumn id=\"66\" name=\"Unit [degree Celsius] (#h; #tUO:0000027; #u)\" xr3:uid=\"{57808D06-1B53-8D41-BEB4-1644A20F7763}\"/><tableColumn id=\"67\" name=\"Term Source REF [degree Celsius] (#h; #tUO:0000027; #u)\" xr3:uid=\"{A7356A0D-0EFD-3D44-9B2F-4566273E79E8}\"/><tableColumn id=\"68\" name=\"Term Accession Number [degree Celsius] (#h; #tUO:0000027; #u)\" xr3:uid=\"{AD21FFE5-445B-CB42-B199-FDD8BAD5635D}\"/><tableColumn dataDxfId=\"8\" id=\"47\" name=\"Characteristics [Temperature Night]\" xr3:uid=\"{2AB78931-B301-314A-9311-2CC2C21128D4}\"/><tableColumn id=\"48\" name=\"Term Source REF [Temperature Night] (#h; #tNFDI4PSO:0000008)\" xr3:uid=\"{3A76D4EC-C74F-0A4B-B340-8EF3D8B86ECB}\"/><tableColumn id=\"49\" name=\"Term Accession Number [Temperature Night] (#h; #tNFDI4PSO:0000008)\" xr3:uid=\"{87034985-F2AC-A145-AA6A-9EDB63F919F9}\"/><tableColumn id=\"69\" name=\"Unit [degree Celsius] (#2; #h; #tUO:0000027; #u)\" xr3:uid=\"{6D54D968-A087-944D-BF64-3AC3D5B08F80}\"/><tableColumn id=\"70\" name=\"Term Source REF [degree Celsius] (#2; #h; #tUO:0000027; #u)\" xr3:uid=\"{AE04B49D-BF47-F34B-B52A-752ECD10B8C7}\"/><tableColumn id=\"71\" name=\"Term Accession Number [degree Celsius] (#2; #h; #tUO:0000027; #u)\" xr3:uid=\"{2A40040A-D450-EF49-AB82-0CEEB3D8A599}\"/><tableColumn dataDxfId=\"7\" id=\"50\" name=\"Characteristics [watering exposure]\" xr3:uid=\"{FC29A38A-1769-7349-A9E5-A5184C62F33A}\"/><tableColumn id=\"51\" name=\"Term Source REF [watering exposure] (#h; #tPECO:0007383)\" xr3:uid=\"{808AA82E-ECE0-AD47-92D3-27D7C93007B7}\"/><tableColumn id=\"52\" name=\"Term Accession Number [watering exposure] (#h; #tPECO:0007383)\" xr3:uid=\"{7C45B5F9-759B-E646-A40E-CB5363DF17A8}\"/><tableColumn dataDxfId=\"6\" id=\"53\" name=\"Characteristics [plant nutrient exposure]\" xr3:uid=\"{70DF5752-BECA-5342-9001-FA7F5271BC54}\"/><tableColumn id=\"54\" name=\"Term Source REF [plant nutrient exposure] (#h; #tPECO:0007241)\" xr3:uid=\"{FD6AD7C0-D253-D04C-8B67-6124B6C5D685}\"/><tableColumn id=\"55\" name=\"Term Accession Number [plant nutrient exposure] (#h; #tPECO:0007241)\" xr3:uid=\"{95BC5752-0FFE-8C4E-BFE3-4FED8B72A648}\"/><tableColumn dataDxfId=\"5\" id=\"56\" name=\"Characteristics [abiotic plant exposure]\" xr3:uid=\"{25540F7D-0E49-F74B-8EF6-6E2314EBD1B1}\"/><tableColumn id=\"57\" name=\"Term Source REF [abiotic plant exposure] (#h; #tPECO:0007191)\" xr3:uid=\"{19EC9032-1152-F940-ADB1-397E054EED4E}\"/><tableColumn id=\"58\" name=\"Term Accession Number [abiotic plant exposure] (#h; #tPECO:0007191)\" xr3:uid=\"{40EA0154-215C-C44C-A5D6-852F5DA9A25C}\"/><tableColumn dataDxfId=\"4\" id=\"59\" name=\"Characteristics [biotic plant exposure]\" xr3:uid=\"{C208C009-B835-0041-9A5C-978B7EABB59C}\"/><tableColumn id=\"60\" name=\"Term Source REF [biotic plant exposure] (#h; #tPECO:0007357)\" xr3:uid=\"{2836636A-01C7-9C4E-9B41-EE6FEE3D7B80}\"/><tableColumn id=\"61\" name=\"Term Accession Number [biotic plant exposure] (#h; #tPECO:0007357)\" xr3:uid=\"{E4A74FCF-2E01-354A-99CE-0602774F8706}\"/><tableColumn dataDxfId=\"3\" id=\"62\" name=\"Characteristics [Time point]\" xr3:uid=\"{12C4BED6-C71D-CC43-B976-AECFD0AF0BCF}\"/><tableColumn id=\"63\" name=\"Term Source REF [Time point] (#h; #tNFDI4PSO:0000034)\" xr3:uid=\"{D78E63DF-76E2-AE4F-A9AA-86CB096E0FD1}\"/><tableColumn id=\"64\" name=\"Term Accession Number [Time point] (#h; #tNFDI4PSO:0000034)\" xr3:uid=\"{4E3E838B-D518-9B41-95E4-AE65DE3C4242}\"/><tableColumn dataDxfId=\"2\" id=\"75\" name=\"Parameter [Sample Collection Method]\" xr3:uid=\"{7FBEC602-6866-EE4C-901B-A620EADE0B55}\"/><tableColumn id=\"76\" name=\"Term Source REF [Sample Collection Method] (#h; #tNFDI4PSO:0000009)\" xr3:uid=\"{082E37E1-9067-2344-AFED-4C0AB2E6608F}\"/><tableColumn id=\"77\" name=\"Term Accession Number [Sample Collection Method] (#h; #tNFDI4PSO:0000009)\" xr3:uid=\"{23CA10DE-9FDA-A94D-84CF-7C5F7143EE90}\"/><tableColumn dataDxfId=\"1\" id=\"78\" name=\"Parameter [Metabolism quenching method]\" xr3:uid=\"{DD36BBD7-7DDE-4B4B-B0E7-E088C0737C72}\"/><tableColumn id=\"79\" name=\"Term Source REF [Metabolism quenching method] (#h; #tNFDI4PSO:0000010)\" xr3:uid=\"{E9A2A827-A7E3-E44F-8034-982633580E8D}\"/><tableColumn id=\"80\" name=\"Term Accession Number [Metabolism quenching method] (#h; #tNFDI4PSO:0000010)\" xr3:uid=\"{ECB588C7-B9C0-8741-BE8A-1CDECCED4ED4}\"/><tableColumn dataDxfId=\"0\" id=\"81\" name=\"Parameter [Sample storage]\" xr3:uid=\"{125431A9-D115-4A4B-A917-1C0A2F361D5C}\"/><tableColumn id=\"82\" name=\"Term Source REF [Sample storage] (#h; #tNFDI4PSO:0000011)\" xr3:uid=\"{A56F8F05-EAAB-FD40-8F2F-AD66235065C4}\"/><tableColumn id=\"83\" name=\"Term Accession Number [Sample storage] (#h; #tNFDI4PSO:0000011)\" xr3:uid=\"{AB05BD40-EE64-3547-80D7-EDD26BB0F77A}\"/></tableColumns><tableStyleInfo name=\"TableStyleMedium7\" showColumnStripes=\"0\" showFirstColumn=\"0\" showLastColumn=\"0\" showRowStripes=\"1\"/></table>'),
(62,	'Plant growth',	'CustomXml',	'<SwateTable Table=\"annotationTable\" Worksheet=\"1SPL01_plants\"><TableValidation DateTime=\"2021-03-12 12:35\" SwateVersion=\"0.4.0\" TableName=\"annotationTable\" Userlist=\"\" WorksheetName=\"1SPL01_plants\"><ColumnValidation ColumnAdress=\"0\" ColumnHeader=\"Source Name\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"1\" ColumnHeader=\"Sample Name\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"2\" ColumnHeader=\"Characteristics [Biological replicate]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"5\" ColumnHeader=\"Characteristics [Organism]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm Organism\"/><ColumnValidation ColumnAdress=\"8\" ColumnHeader=\"Characteristics [Genotype]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"11\" ColumnHeader=\"Characteristics [Organism part]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm Organism part\"/><ColumnValidation ColumnAdress=\"14\" ColumnHeader=\"Characteristics [age]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"17\" ColumnHeader=\"Characteristics [study type]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm study type\"/><ColumnValidation ColumnAdress=\"20\" ColumnHeader=\"Characteristics [plant growth medium exposure]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm plant growth medium exposure\"/><ColumnValidation ColumnAdress=\"23\" ColumnHeader=\"Characteristics [growth plot design]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm growth plot design\"/><ColumnValidation ColumnAdress=\"26\" ColumnHeader=\"Characteristics [Growth day length]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"29\" ColumnHeader=\"Characteristics [light intensity exposure]\" Importance=\"4\" Unit=\"microeinstein per square meter per second\" ValidationFormat=\"UnitTerm microeinstein per square meter per second\"/><ColumnValidation ColumnAdress=\"35\" ColumnHeader=\"Characteristics [Humidity Day]\" Importance=\"4\" Unit=\"percent\" ValidationFormat=\"UnitTerm percent\"/><ColumnValidation ColumnAdress=\"41\" ColumnHeader=\"Characteristics [Humidity Night]\" Importance=\"4\" Unit=\"percent\" ValidationFormat=\"UnitTerm percent\"/><ColumnValidation ColumnAdress=\"47\" ColumnHeader=\"Characteristics [Temperature Day]\" Importance=\"4\" Unit=\"degree Celsius\" ValidationFormat=\"UnitTerm degree Celsius\"/><ColumnValidation ColumnAdress=\"53\" ColumnHeader=\"Characteristics [Temperature Night]\" Importance=\"4\" Unit=\"degree Celsius\" ValidationFormat=\"UnitTerm degree Celsius\"/><ColumnValidation ColumnAdress=\"59\" ColumnHeader=\"Characteristics [watering exposure]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"62\" ColumnHeader=\"Characteristics [plant nutrient exposure]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm plant nutrient exposure\"/><ColumnValidation ColumnAdress=\"65\" ColumnHeader=\"Characteristics [abiotic plant exposure]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"68\" ColumnHeader=\"Characteristics [biotic plant exposure]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"71\" ColumnHeader=\"Characteristics [Time point]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"74\" ColumnHeader=\"Parameter [Sample Collection Method]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"77\" ColumnHeader=\"Parameter [Metabolism quenching method]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"80\" ColumnHeader=\"Parameter [Sample storage]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"None\"/></TableValidation></SwateTable>'),
(65,	'RNA extraction',	'TableXml',	'<?xml version=\"1.0\" ?><table displayName=\"annotationTable1\" id=\"3\" mc:Ignorable=\"xr xr3\" name=\"annotationTable1\" ref=\"A2:W3\" totalsRowShown=\"0\" xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:xr=\"http://schemas.microsoft.com/office/spreadsheetml/2014/revision\" xmlns:xr3=\"http://schemas.microsoft.com/office/spreadsheetml/2016/revision3\" xr:uid=\"{76206DCD-B1F6-C24E-BAE1-D2FB91ABFB99}\"><autoFilter ref=\"A2:W3\" xr:uid=\"{832614FC-4AE4-BA4A-9BF7-BFA6BCCB799C}\"/><tableColumns count=\"23\"><tableColumn id=\"1\" name=\"Source Name\" xr3:uid=\"{DECFF8D0-3CE7-FE4A-AE75-815E572E9742}\"/><tableColumn id=\"2\" name=\"Sample Name\" xr3:uid=\"{C40C8FCE-0AA0-4049-8114-5B5645A6E93C}\"/><tableColumn dataDxfId=\"4\" id=\"12\" name=\"Parameter [Bio entity]\" xr3:uid=\"{D487B3C2-FCC9-9748-9A8A-0D6A83AEB324}\"/><tableColumn id=\"13\" name=\"Term Source REF [Bio entity] (#h; #tNFDI4PSO:0000012)\" xr3:uid=\"{4B0FE9CC-E2ED-AF4A-90D4-FAB6DBED33EE}\"/><tableColumn id=\"14\" name=\"Term Accession Number [Bio entity] (#h; #tNFDI4PSO:0000012)\" xr3:uid=\"{522BBFA9-4A80-D745-87FE-9CAA1BAF6807}\"/><tableColumn dataDxfId=\"3\" id=\"15\" name=\"Parameter [Biosource amount]\" xr3:uid=\"{6607B5DF-7714-E943-8E91-48985930C54B}\"/><tableColumn id=\"16\" name=\"Term Source REF [Biosource amount] (#h; #tNFDI4PSO:0000013)\" xr3:uid=\"{78C0B04C-E73C-FB43-A41E-494193F7A48C}\"/><tableColumn id=\"17\" name=\"Term Accession Number [Biosource amount] (#h; #tNFDI4PSO:0000013)\" xr3:uid=\"{2D769069-CC3A-EE43-BE2D-4B768EFAD693}\"/><tableColumn id=\"21\" name=\"Unit [milligram] (#h; #tUO:0000022; #u)\" xr3:uid=\"{8DC2DF91-CE94-E94A-A5B4-02752EE2C776}\"/><tableColumn id=\"22\" name=\"Term Source REF [milligram] (#h; #tUO:0000022; #u)\" xr3:uid=\"{5D24ADED-ED35-9945-B1D3-AD774AF07057}\"/><tableColumn id=\"23\" name=\"Term Accession Number [milligram] (#h; #tUO:0000022; #u)\" xr3:uid=\"{572598D7-894F-5149-8C6D-4A78BC6C27F2}\"/><tableColumn dataDxfId=\"2\" id=\"18\" name=\"Parameter [Extraction Kit]\" xr3:uid=\"{FFC95040-19C9-734F-A4E3-B5611916C3A4}\"/><tableColumn id=\"19\" name=\"Term Source REF [Extraction Kit] (#h; #tNFDI4PSO:0000014)\" xr3:uid=\"{FEFD66AC-DF32-8141-95E1-36BD2B6F792C}\"/><tableColumn id=\"20\" name=\"Term Accession Number [Extraction Kit] (#h; #tNFDI4PSO:0000014)\" xr3:uid=\"{1149D66F-4ABD-6846-8CB4-4DC6BD6B8DC2}\"/><tableColumn dataDxfId=\"1\" id=\"3\" name=\"Parameter [Extraction buffer]\" xr3:uid=\"{1BA7D7BD-0172-D24F-8453-ED3666AE07C6}\"/><tableColumn id=\"4\" name=\"Term Source REF [Extraction buffer] (#h; #tNFDI4PSO:0000050)\" xr3:uid=\"{5A08E2A9-B247-A349-8E06-07F3569AE22E}\"/><tableColumn id=\"5\" name=\"Term Accession Number [Extraction buffer] (#h; #tNFDI4PSO:0000050)\" xr3:uid=\"{25876647-E95A-044B-9E30-DA15780D6980}\"/><tableColumn dataDxfId=\"0\" id=\"6\" name=\"Parameter [Extraction buffer volume]\" xr3:uid=\"{D751E028-9AD1-2742-A18D-568661A5D639}\"/><tableColumn id=\"7\" name=\"Term Source REF [Extraction buffer volume] (#h; #tNFDI4PSO:0000051)\" xr3:uid=\"{08853ADC-69EB-484C-B596-6F5F8AAFBFA9}\"/><tableColumn id=\"8\" name=\"Term Accession Number [Extraction buffer volume] (#h; #tNFDI4PSO:0000051)\" xr3:uid=\"{2F5A3FD0-B97E-3546-B233-668066642CFA}\"/><tableColumn id=\"9\" name=\"Unit [microliter] (#h; #tUO:0000101; #u)\" xr3:uid=\"{818D6D68-D03E-E947-BFFB-4028187F6132}\"/><tableColumn id=\"10\" name=\"Term Source REF [microliter] (#h; #tUO:0000101; #u)\" xr3:uid=\"{9027E325-ED31-A045-9A35-13FF444EB372}\"/><tableColumn id=\"11\" name=\"Term Accession Number [microliter] (#h; #tUO:0000101; #u)\" xr3:uid=\"{3A8F4E34-DB64-FD49-BA49-A79AFA7B8080}\"/></tableColumns><tableStyleInfo name=\"TableStyleMedium7\" showColumnStripes=\"0\" showFirstColumn=\"0\" showLastColumn=\"0\" showRowStripes=\"1\"/></table>'),
(66,	'RNA extraction',	'CustomXml',	'<SwateTable Table=\"annotationTable1\" Worksheet=\"2EXT01_RNA\"><TableValidation DateTime=\"2021-03-12 14:48\" SwateVersion=\"0.4.0\" TableName=\"annotationTable1\" Userlist=\"\" WorksheetName=\"2EXT01_RNA\"><ColumnValidation ColumnAdress=\"0\" ColumnHeader=\"Source Name\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"1\" ColumnHeader=\"Sample Name\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"2\" ColumnHeader=\"Parameter [Bio entity]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm Bio entity\"/><ColumnValidation ColumnAdress=\"5\" ColumnHeader=\"Parameter [Biosource amount]\" Importance=\"4\" Unit=\"milligram\" ValidationFormat=\"UnitTerm milligram\"/><ColumnValidation ColumnAdress=\"11\" ColumnHeader=\"Parameter [Extraction Kit]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"14\" ColumnHeader=\"Parameter [Extraction buffer]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"17\" ColumnHeader=\"Parameter [Extraction buffer volume]\" Importance=\"4\" Unit=\"microliter\" ValidationFormat=\"UnitTerm microliter\"/></TableValidation></SwateTable>'),
(71,	'Metabolite Extraction',	'TableXml',	'<?xml version=\"1.0\" ?><table displayName=\"annotationTable\" id=\"1\" mc:Ignorable=\"xr xr3\" name=\"annotationTable\" ref=\"A2:T3\" totalsRowShown=\"0\" xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:xr=\"http://schemas.microsoft.com/office/spreadsheetml/2014/revision\" xmlns:xr3=\"http://schemas.microsoft.com/office/spreadsheetml/2016/revision3\" xr:uid=\"{1D2F4E0D-C0CD-5F4A-8873-824BF4046AF1}\"><autoFilter ref=\"A2:T3\" xr:uid=\"{B8CBCCDE-BB9A-1943-AF13-CF08D39F25EB}\"/><tableColumns count=\"20\"><tableColumn id=\"1\" name=\"Source Name\" xr3:uid=\"{3998B63E-8A32-8B43-A9F8-6B28BA709419}\"/><tableColumn id=\"2\" name=\"Sample Name\" xr3:uid=\"{B0972144-49CE-1D49-B224-1CC170522482}\"/><tableColumn dataDxfId=\"3\" id=\"3\" name=\"Parameter [Bio entity]\" xr3:uid=\"{77AF736E-56D5-7F44-A162-A7D79B145D0A}\"/><tableColumn id=\"4\" name=\"Term Source REF [Bio entity] (#h; #tNFDI4PSO:0000012)\" xr3:uid=\"{5AFF8057-B228-184D-957B-38ADC417EEC9}\"/><tableColumn id=\"5\" name=\"Term Accession Number [Bio entity] (#h; #tNFDI4PSO:0000012)\" xr3:uid=\"{A24CE6BE-0369-E64C-BAC0-E00C061D43E7}\"/><tableColumn dataDxfId=\"0\" id=\"6\" name=\"Parameter [Biosource amount]\" xr3:uid=\"{E1ADB4C9-3F18-7A4B-9E3D-7EC6E004F32D}\"/><tableColumn id=\"7\" name=\"Term Source REF [Biosource amount] (#h; #tNFDI4PSO:0000013)\" xr3:uid=\"{493F6E04-5CE7-5246-BB0D-91442E111E87}\"/><tableColumn id=\"8\" name=\"Term Accession Number [Biosource amount] (#h; #tNFDI4PSO:0000013)\" xr3:uid=\"{5FEEE5BD-D15D-9348-B37F-A958A19A55CB}\"/><tableColumn id=\"18\" name=\"Unit [microgram] (#h; #tUO:0000023; #u)\" xr3:uid=\"{305FF980-E111-FF46-AF42-5DD9F03116E9}\"/><tableColumn id=\"19\" name=\"Term Source REF [microgram] (#h; #tUO:0000023; #u)\" xr3:uid=\"{957B0452-8A50-8341-95AC-800254CDEF5C}\"/><tableColumn id=\"20\" name=\"Term Accession Number [microgram] (#h; #tUO:0000023; #u)\" xr3:uid=\"{22B23873-D79F-7846-92D1-0D9334B913E8}\"/><tableColumn dataDxfId=\"2\" id=\"9\" name=\"Parameter [Extraction buffer]\" xr3:uid=\"{B43D5B8F-36C5-5D43-ABCD-34BA91C8BFA0}\"/><tableColumn id=\"10\" name=\"Term Source REF [Extraction buffer] (#h; #tNFDI4PSO:0000050)\" xr3:uid=\"{2D020EA2-7A97-5044-9CFC-4ACFB6D2C914}\"/><tableColumn id=\"11\" name=\"Term Accession Number [Extraction buffer] (#h; #tNFDI4PSO:0000050)\" xr3:uid=\"{16B4A7CE-8B30-084E-9625-081F90B2C059}\"/><tableColumn dataDxfId=\"1\" id=\"12\" name=\"Parameter [Extraction buffer volume]\" xr3:uid=\"{10AF58E4-15A1-F344-AC18-924EF5AFE263}\"/><tableColumn id=\"13\" name=\"Term Source REF [Extraction buffer volume] (#h; #tNFDI4PSO:0000051)\" xr3:uid=\"{9C138AE8-01A3-644C-A8FB-4F1AE6122738}\"/><tableColumn id=\"14\" name=\"Term Accession Number [Extraction buffer volume] (#h; #tNFDI4PSO:0000051)\" xr3:uid=\"{776BA372-248E-A346-9560-AC99C0025955}\"/><tableColumn id=\"15\" name=\"Unit [microliter] (#h; #tUO:0000101; #u)\" xr3:uid=\"{62EB9B8C-50C0-6846-8385-0ECF0EEA3C7A}\"/><tableColumn id=\"16\" name=\"Term Source REF [microliter] (#h; #tUO:0000101; #u)\" xr3:uid=\"{72DC9DFD-4689-034A-BBD8-638D9FD59658}\"/><tableColumn id=\"17\" name=\"Term Accession Number [microliter] (#h; #tUO:0000101; #u)\" xr3:uid=\"{DA5CEEFD-FC2F-5F4E-92BF-2EDE31FDC7EF}\"/></tableColumns><tableStyleInfo name=\"TableStyleMedium7\" showColumnStripes=\"0\" showFirstColumn=\"0\" showLastColumn=\"0\" showRowStripes=\"1\"/></table>'),
(72,	'Metabolite Extraction',	'CustomXml',	'<SwateTable Table=\"annotationTable\" Worksheet=\"2EXT03_metabolites\"><TableValidation DateTime=\"2021-03-12 14:58\" SwateVersion=\"0.4.5\" TableName=\"annotationTable\" Userlist=\"\" WorksheetName=\"2EXT03_metabolites\"><ColumnValidation ColumnAdress=\"0\" ColumnHeader=\"Source Name\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"1\" ColumnHeader=\"Sample Name\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"2\" ColumnHeader=\"Parameter [Bio entity]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"5\" ColumnHeader=\"Parameter [Biosource amount]\" Importance=\"4\" Unit=\"microgram\" ValidationFormat=\"UnitTerm microgram\"/><ColumnValidation ColumnAdress=\"11\" ColumnHeader=\"Parameter [Extraction buffer]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"14\" ColumnHeader=\"Parameter [Extraction buffer volume]\" Importance=\"4\" Unit=\"microliter\" ValidationFormat=\"UnitTerm microliter\"/></TableValidation></SwateTable>'),
(73,	'Metabolomics Assay',	'TableXml',	'<?xml version=\"1.0\" ?><table displayName=\"annotationTable\" id=\"2\" mc:Ignorable=\"xr xr3\" name=\"annotationTable\" ref=\"A2:AU3\" totalsRowShown=\"0\" xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:xr=\"http://schemas.microsoft.com/office/spreadsheetml/2014/revision\" xmlns:xr3=\"http://schemas.microsoft.com/office/spreadsheetml/2016/revision3\" xr:uid=\"{2BA91F90-A206-C24A-B071-E1351392860E}\"><autoFilter ref=\"A2:AU3\" xr:uid=\"{155D8245-62B7-F340-A809-633676FF14D5}\"/><tableColumns count=\"47\"><tableColumn id=\"1\" name=\"Source Name\" xr3:uid=\"{BBBF64BA-87C6-D34E-A6CD-4299F4D962C8}\"/><tableColumn id=\"2\" name=\"Sample Name\" xr3:uid=\"{14AC94B4-25B3-E44B-8B75-F5DA7ADF4505}\"/><tableColumn dataDxfId=\"14\" id=\"3\" name=\"Parameter [MS sample post-extraction]\" xr3:uid=\"{DE8A40D7-A9A3-6D44-A444-4CDA584115FB}\"/><tableColumn id=\"4\" name=\"Term Source REF [MS sample post-extraction] (#h; #tNFDI4PSO:0000043)\" xr3:uid=\"{91DB6965-9407-084F-B87C-167B363C8F64}\"/><tableColumn id=\"5\" name=\"Term Accession Number [MS sample post-extraction] (#h; #tNFDI4PSO:0000043)\" xr3:uid=\"{8F068F1B-0799-934A-A950-0CCA864FA729}\"/><tableColumn dataDxfId=\"13\" id=\"6\" name=\"Parameter [MS sample resuspension]\" xr3:uid=\"{C6859ADD-6217-6046-B9CE-0E2C44AEB18F}\"/><tableColumn id=\"7\" name=\"Term Source REF [MS sample resuspension] (#h; #tNFDI4PSO:0000044)\" xr3:uid=\"{7FBCD097-8CAE-F140-921C-B77023BC4AE2}\"/><tableColumn id=\"8\" name=\"Term Accession Number [MS sample resuspension] (#h; #tNFDI4PSO:0000044)\" xr3:uid=\"{81B6A235-8405-DD4A-8C68-E37AC0D99B1B}\"/><tableColumn dataDxfId=\"12\" id=\"9\" name=\"Parameter [MS sample type]\" xr3:uid=\"{0D61368D-553F-1949-B1E3-71D3E20AD860}\"/><tableColumn id=\"10\" name=\"Term Source REF [MS sample type] (#h; #tNFDI4PSO:0000045)\" xr3:uid=\"{6179952F-0BCA-CF4E-944F-EB19C4832C67}\"/><tableColumn id=\"11\" name=\"Term Accession Number [MS sample type] (#h; #tNFDI4PSO:0000045)\" xr3:uid=\"{994D6D0B-5285-DE4E-95A4-E184360E374C}\"/><tableColumn dataDxfId=\"11\" id=\"12\" name=\"Parameter [MS derivatization]\" xr3:uid=\"{B56199D0-4A72-9A48-A183-6470EDD554B1}\"/><tableColumn id=\"13\" name=\"Term Source REF [MS derivatization] (#h; #tNFDI4PSO:0000052)\" xr3:uid=\"{2D30CB05-A2E2-F747-B469-07C250031A33}\"/><tableColumn id=\"14\" name=\"Term Accession Number [MS derivatization] (#h; #tNFDI4PSO:0000052)\" xr3:uid=\"{6110C472-0713-954C-9FD7-812784023EE1}\"/><tableColumn dataDxfId=\"10\" id=\"18\" name=\"Parameter [Chromatography instrument model]\" xr3:uid=\"{F21D0BDF-9000-5547-AD8A-1BE6C6EB09E6}\"/><tableColumn id=\"19\" name=\"Term Source REF [Chromatography instrument model] (#h; #tNFDI4PSO:0000046)\" xr3:uid=\"{93F305FB-AF63-0547-9A9E-9316392B763C}\"/><tableColumn id=\"20\" name=\"Term Accession Number [Chromatography instrument model] (#h; #tNFDI4PSO:0000046)\" xr3:uid=\"{036AA641-2BC0-5847-B2EF-7FA1822B2E7C}\"/><tableColumn dataDxfId=\"9\" id=\"21\" name=\"Parameter [Chromatography autosampler model]\" xr3:uid=\"{81B5AB82-D2E2-C740-93BA-2766BD270F4C}\"/><tableColumn id=\"22\" name=\"Term Source REF [Chromatography autosampler model] (#h; #tNFDI4PSO:0000047)\" xr3:uid=\"{9B4B8450-0F52-524D-BC1F-8E8E4F80A6BE}\"/><tableColumn id=\"23\" name=\"Term Accession Number [Chromatography autosampler model] (#h; #tNFDI4PSO:0000047)\" xr3:uid=\"{2C883FF4-82A2-EF44-8B01-97DEF7569A6D}\"/><tableColumn dataDxfId=\"8\" id=\"24\" name=\"Parameter [Chromatography column type]\" xr3:uid=\"{512E4A1B-6AF1-BB4D-8962-A4698189D07B}\"/><tableColumn id=\"25\" name=\"Term Source REF [Chromatography column type] (#h; #tNFDI4PSO:0000053)\" xr3:uid=\"{B270572B-8B22-1B4F-9EC7-50CCCD074C18}\"/><tableColumn id=\"26\" name=\"Term Accession Number [Chromatography column type] (#h; #tNFDI4PSO:0000053)\" xr3:uid=\"{FD917BD1-F453-8F4C-BBB1-50D7883F34ED}\"/><tableColumn dataDxfId=\"7\" id=\"27\" name=\"Parameter [Chromatography guard column model]\" xr3:uid=\"{17F7E68D-3B88-FF43-A4B3-399DCFB2F59B}\"/><tableColumn id=\"28\" name=\"Term Source REF [Chromatography guard column model] (#h; #tNFDI4PSO:0000049)\" xr3:uid=\"{A4809FDB-124E-3C42-BAE1-11373D15BF08}\"/><tableColumn id=\"29\" name=\"Term Accession Number [Chromatography guard column model] (#h; #tNFDI4PSO:0000049)\" xr3:uid=\"{FA85BE75-6B6C-7140-AC46-8BA24514D954}\"/><tableColumn dataDxfId=\"6\" id=\"30\" name=\"Parameter [scan polarity]\" xr3:uid=\"{183360A2-948E-BB46-9A5B-9314170BA82F}\"/><tableColumn id=\"31\" name=\"Term Source REF [scan polarity] (#h; #tMS:1000465)\" xr3:uid=\"{8CE758DF-293A-5B43-8923-E07BBEA300B9}\"/><tableColumn id=\"32\" name=\"Term Accession Number [scan polarity] (#h; #tMS:1000465)\" xr3:uid=\"{C2D1CF91-64E8-8844-B84E-0198C9EBC687}\"/><tableColumn dataDxfId=\"5\" id=\"33\" name=\"Parameter [scan window lower limit]\" xr3:uid=\"{1BE5C769-A04D-F344-AF0D-86BBBDED416E}\"/><tableColumn id=\"34\" name=\"Term Source REF [scan window lower limit] (#h; #tMS:1000501)\" xr3:uid=\"{720A408D-0D3D-5A47-9C32-FA091FF2BBA5}\"/><tableColumn id=\"35\" name=\"Term Accession Number [scan window lower limit] (#h; #tMS:1000501)\" xr3:uid=\"{10AC5530-C162-4F4B-B33D-69144D995141}\"/><tableColumn dataDxfId=\"4\" id=\"36\" name=\"Parameter [scan window upper limit]\" xr3:uid=\"{C8558B98-C98A-A244-A439-51E3B9875A8F}\"/><tableColumn id=\"37\" name=\"Term Source REF [scan window upper limit] (#h; #tMS:1000500)\" xr3:uid=\"{8001FAB7-F714-324C-B592-D83361B52D1A}\"/><tableColumn id=\"38\" name=\"Term Accession Number [scan window upper limit] (#h; #tMS:1000500)\" xr3:uid=\"{09050567-9154-5644-9F7E-7A35F81C36C5}\"/><tableColumn dataDxfId=\"3\" id=\"39\" name=\"Parameter [instrument model]\" xr3:uid=\"{7C0DFAF7-046E-534F-9C57-89FEE3E72208}\"/><tableColumn id=\"40\" name=\"Term Source REF [instrument model] (#h; #tMS:1000031)\" xr3:uid=\"{3B19E320-D898-6947-A400-B7D35D58F1A6}\"/><tableColumn id=\"41\" name=\"Term Accession Number [instrument model] (#h; #tMS:1000031)\" xr3:uid=\"{14CC0697-3DFB-3447-B1FD-767F08108295}\"/><tableColumn dataDxfId=\"2\" id=\"42\" name=\"Parameter [ionization type]\" xr3:uid=\"{97CC8885-D3E6-AA4E-A1AA-E031764C9196}\"/><tableColumn id=\"43\" name=\"Term Source REF [ionization type] (#h; #tMS:1000008)\" xr3:uid=\"{5BD69720-0711-3440-BFAD-856D20CC0859}\"/><tableColumn id=\"44\" name=\"Term Accession Number [ionization type] (#h; #tMS:1000008)\" xr3:uid=\"{ED78172F-0957-0649-9712-53DC91AAEF41}\"/><tableColumn dataDxfId=\"1\" id=\"45\" name=\"Parameter [mass analyzer type]\" xr3:uid=\"{209174F2-9485-7D4F-9577-999720963CA1}\"/><tableColumn id=\"46\" name=\"Term Source REF [mass analyzer type] (#h; #tMS:1000443)\" xr3:uid=\"{E1C36D22-2A7B-F04A-AF75-00C1C8C9CAF4}\"/><tableColumn id=\"47\" name=\"Term Accession Number [mass analyzer type] (#h; #tMS:1000443)\" xr3:uid=\"{F695DECA-D1D9-A542-8FEF-479135275C4A}\"/><tableColumn dataDxfId=\"0\" id=\"48\" name=\"Parameter [detector type]\" xr3:uid=\"{B0DDCF73-7408-C247-9E67-7CBF0B943A49}\"/><tableColumn id=\"49\" name=\"Term Source REF [detector type] (#h; #tMS:1000026)\" xr3:uid=\"{9E7C28E6-2B09-5A4D-BEA2-E89CA9B02622}\"/><tableColumn id=\"50\" name=\"Term Accession Number [detector type] (#h; #tMS:1000026)\" xr3:uid=\"{2EAE4DF7-6E4D-8044-9A56-E7115F15BB64}\"/></tableColumns><tableStyleInfo name=\"TableStyleMedium7\" showColumnStripes=\"0\" showFirstColumn=\"0\" showLastColumn=\"0\" showRowStripes=\"1\"/></table>'),
(74,	'Metabolomics Assay',	'CustomXml',	'<SwateTable Table=\"annotationTable\" Worksheet=\"3ASY03_Metabolomics\"><TableValidation DateTime=\"2021-03-12 15:26\" SwateVersion=\"0.4.5\" TableName=\"annotationTable\" Userlist=\"\" WorksheetName=\"3ASY03_Metabolomics\"><ColumnValidation ColumnAdress=\"0\" ColumnHeader=\"Source Name\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"1\" ColumnHeader=\"Sample Name\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"2\" ColumnHeader=\"Parameter [MS sample post-extraction]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"5\" ColumnHeader=\"Parameter [MS sample resuspension]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"8\" ColumnHeader=\"Parameter [MS sample type]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"11\" ColumnHeader=\"Parameter [MS derivatization]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm MS derivatization\"/><ColumnValidation ColumnAdress=\"14\" ColumnHeader=\"Parameter [Chromatography instrument model]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm Chromatography instrument model\"/><ColumnValidation ColumnAdress=\"17\" ColumnHeader=\"Parameter [Chromatography autosampler model]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm Chromatography autosampler model\"/><ColumnValidation ColumnAdress=\"20\" ColumnHeader=\"Parameter [Chromatography column type]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm Chromatography column type\"/><ColumnValidation ColumnAdress=\"23\" ColumnHeader=\"Parameter [Chromatography guard column model]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"26\" ColumnHeader=\"Parameter [scan polarity]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm scan polarity\"/><ColumnValidation ColumnAdress=\"29\" ColumnHeader=\"Parameter [scan window lower limit]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"32\" ColumnHeader=\"Parameter [scan window upper limit]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"35\" ColumnHeader=\"Parameter [instrument model]\" Importance=\"3\" Unit=\"None\" ValidationFormat=\"OntologyTerm instrument model\"/><ColumnValidation ColumnAdress=\"38\" ColumnHeader=\"Parameter [ionization type]\" Importance=\"4\" Unit=\"None\" ValidationFormat=\"OntologyTerm ionization type\"/><ColumnValidation ColumnAdress=\"41\" ColumnHeader=\"Parameter [mass analyzer type]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/><ColumnValidation ColumnAdress=\"44\" ColumnHeader=\"Parameter [detector type]\" Importance=\"None\" Unit=\"None\" ValidationFormat=\"None\"/></TableValidation></SwateTable>');

DROP TABLE IF EXISTS `Term`;
CREATE TABLE `Term` (
  `Accession` varchar(128) NOT NULL,
  `FK_OntologyName` varchar(256) NOT NULL,
  `Name` varchar(1024) NOT NULL,
  `Definition` varchar(2048) NOT NULL,
  `XRefValueType` varchar(256) DEFAULT NULL,
  `IsObsolete` tinyint(1) NOT NULL,
  UNIQUE KEY `IX_Term_Accession` (`Accession`),
  KEY `Ind_Accession` (`Accession`),
  KEY `term_Name` (`Name`(255)),
  KEY `FK_OntologyName` (`FK_OntologyName`),
  FULLTEXT KEY `Name` (`Name`),
  FULLTEXT KEY `Definition` (`Definition`),
  CONSTRAINT `Term_ibfk_1` FOREIGN KEY (`FK_OntologyName`) REFERENCES `Ontology` (`Name`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


DROP TABLE IF EXISTS `TermRelationship`;
CREATE TABLE `TermRelationship` (
  `ID` bigint(20) NOT NULL AUTO_INCREMENT,
  `FK_TermAccession` varchar(128) NOT NULL,
  `RelationshipType` varchar(64) NOT NULL,
  `FK_TermAccession_Related` varchar(128) NOT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `IX_TermRelationship` (`FK_TermAccession`,`FK_TermAccession_Related`,`RelationshipType`),
  KEY `Ind_FK_TermRelationship_Term1` (`FK_TermAccession_Related`),
  KEY `Ind_FK_TermID` (`FK_TermAccession`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


-- 2021-05-12 07:23:36
